# Runbook — Secrets de produção (SOPS + age)

[English](./secrets.md) · **Português**

**Audiência:** qualquer humano com acesso ao repositório e à droplet de
produção, mesmo sem contexto prévio do projeto.

## 1. Por que isso existe

Antes deste runbook, o `.env` de produção vivia só na droplet, em texto
plano, sem histórico, sem revisão e sem backup versionado. Agora os
segredos de produção do **backend** (DB, JWT, e-mail, New Relic) ficam em
`secrets/prod.enc.env`, **criptografados** com [SOPS](https://github.com/getsops/sops)
usando [age](https://github.com/FiloSottile/age), versionados no git como
qualquer outro arquivo — revisáveis num PR (as chaves ficam legíveis, só os
valores são criptografados), mas ilegíveis sem a chave privada age.

**Fora de escopo:** Admin Panel (SPA estático, config pública) e site
institucional (`NEXT_PUBLIC_*`, público por design) não têm segredos
server-side reais — não usam SOPS.

## 2. Onde cada peça vive

| Peça | Local | Quem usa |
|---|---|---|
| `.sops.yaml` | raiz do repo (versionado) | `sops` resolve automaticamente qual chave usar pra criptografar `secrets/*.enc.env` |
| `secrets/prod.enc.env` | raiz do repo, pasta `secrets/` (versionado, **criptografado**) | qualquer um pode ler o arquivo no git; só quem tem a chave privada decripta os valores |
| Chave pública age | dentro do `.sops.yaml` (versionada) | não é segredo — é só "para quem criptografar" |
| Chave privada age | **NUNCA no git.** GitHub Actions secret `SOPS_AGE_KEY` + droplet em `~/.config/sops/age/keys.txt` | CI/CD (se precisar decriptar em pipeline) e a droplet (decripta no deploy) |

## 3. Gerar a chave age real (uma única vez, localmente)

```bash
age-keygen -o age.key
```

Saída esperada:
```
Public key: age1...
```

- A **chave pública** (`age1...`) vai no `.sops.yaml` (substituindo a
  fictícia que está lá hoje — ver §6).
- A **chave privada** (conteúdo completo do arquivo `age.key`, incluindo o
  comentário `# created:`/`# public key:`) vai em dois lugares, **nunca no
  git**:
  1. GitHub Actions secret `SOPS_AGE_KEY` (Settings → Secrets and
     variables → Actions → New repository secret).
  2. `~/.config/sops/age/keys.txt` na droplet (permissão `600`, dono
     `deploy`) — hoje injetado automaticamente via Terraform/cloud-init
     (`infra/terraform/cloud-init.yaml`, variável `age_private_key` em
     `infra/terraform/variables.tf`), então normalmente você só precisa
     passar o conteúdo como `TF_VAR_age_private_key` (ou no
     `terraform.tfvars`, nunca commitado) antes do `terraform apply`.
- Apague `age.key` do disco local depois de guardar a chave privada nos
  dois lugares acima (ou guarde-o num cofre de senhas pessoal — nunca numa
  pasta sincronizada/versionada).

## 4. Editar um segredo existente

Pré-requisito: a chave privada real precisa estar acessível localmente
(`~/.config/sops/age/keys.txt` ou `SOPS_AGE_KEY_FILE=/caminho/para/age.key`).

```bash
sops secrets/prod.enc.env
```

Abre o conteúdo decriptado no `$EDITOR`; ao salvar e fechar, o SOPS
re-criptografa e sobrescreve o arquivo no disco automaticamente. Comitar a
mudança normalmente (`git add secrets/prod.enc.env && git commit`).

## 5. Decriptar no deploy (droplet)

O job de deploy/`sync-config.sh` (plano 22) envia `secrets/prod.enc.env`
para a droplet. Lá, com a chave privada já instalada:

```bash
sops --decrypt secrets/prod.enc.env > .env
chmod 600 .env
```

**Nunca** logar o conteúdo decriptado em nenhum step de CI/CD (mascarar
valores com `::add-mask::` se precisar manipular programaticamente, e
nunca usar `set -x`/`cat .env` num step sem `continue-on-error: false`
explicitamente revisado).

## 6. Trocar a chave fictícia atual por uma real (primeira vez)

Hoje (estado deste commit), `.sops.yaml` e `secrets/prod.enc.env` usam uma
chave age **fictícia**, gerada só para validar o mecanismo (ver aviso em
`secrets/README.md`). Antes do primeiro deploy real usando este fluxo:

1. Gerar a chave real (§3).
2. Editar `.sops.yaml`, trocando a chave pública fictícia pela real.
3. Rodar `sops updatekeys secrets/prod.enc.env` — **isso só funciona se
   você ainda tiver a chave privada fictícia disponível localmente** no
   momento da troca (o `updatekeys` decripta com a chave antiga e
   re-criptografa para a(s) nova(s) do `.sops.yaml`). Caso a chave
   fictícia já tenha sido descartada, mais simples: rodar
   `sops --decrypt` manualmente não vai funcionar (chave já descartada,
   exatamente como deveria) — nesse caso, apague `secrets/prod.enc.env` e
   recrie do zero com `sops --encrypt` a partir de um rascunho preenchido
   com os valores reais (ver `secrets/README.md`).
4. Preencher os valores reais (`sops secrets/prod.enc.env`, ver §4):
   senha real do Postgres, `JWT_KEY` real (≥32 chars aleatórios), SMTP
   real, `NR_LICENSE_KEY` real.
5. Guardar a chave privada real no GitHub secret `SOPS_AGE_KEY` e na
   droplet (§3).

## 7. Rotação periódica de segredos de aplicação (JWT/DB/Email/NR)

Recomendação: revisar/rotacionar a cada 90 dias ou após qualquer suspeita
de exposição (ex. log acidental, laptop comprometido, saída de alguém com
acesso).

1. Gerar o novo valor na fonte (ex. nova senha do Postgres no próprio
   Postgres, novo `JWT_KEY` aleatório).
2. `sops secrets/prod.enc.env` → atualizar o valor → salvar → commitar.
3. No próximo deploy, a droplet decripta a versão nova; reiniciar o
   serviço `api` (não é hot-reload — o `.env` só é lido na inicialização
   do container).
4. **Cuidado com `JWT_KEY`:** rotacionar invalida todos os tokens JWT já
   emitidos — usuários logados precisam logar de novo. Se a rotação não
   for por suspeita de comprometimento, agendar para um horário de baixo uso.

## 8. Rotação da própria chave age

Ver `secrets/README.md`, seção "Rotação da própria chave age" — mesmo
runbook, mantido lá para ficar perto do `.sops.yaml`/`secrets/` que ele
referencia diretamente.

## 9. Pré-requisitos manuais (antes de usar este fluxo em produção)

- [ ] Gerar a chave age real (`age-keygen`) **localmente, numa máquina
      confiável**.
- [ ] Guardar a chave privada real em `SOPS_AGE_KEY` (GitHub) e na droplet.
- [ ] Atualizar `.sops.yaml` com a chave pública real.
- [ ] Preencher `secrets/prod.enc.env` com os valores reais de produção
      (senha do Postgres, `JWT_KEY`, credenciais SMTP, `NR_LICENSE_KEY`).
- [ ] Confirmar que nenhum `.env` plaintext legado continua na droplet
      fora do fluxo `sops --decrypt > .env` (remover qualquer cópia
      manual antiga).
