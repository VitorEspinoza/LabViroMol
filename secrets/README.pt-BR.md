# secrets/ — segredos de produção criptografados (SOPS + age)

[English](./README.md) · **Português**

Esta pasta versiona segredos de produção do backend **criptografados** com
[SOPS](https://github.com/getsops/sops) usando [age](https://github.com/FiloSottile/age)
como backend de criptografia. O runbook completo está em
[`docs/runbooks/secrets.md`](../docs/runbooks/secrets.pt-BR.md).

## O que pode ir aqui

- `prod.enc.env` — único arquivo hoje. Variáveis de ambiente de produção do
  backend (DB, JWT, Email, observabilidade), no formato `dotenv`,
  **criptografadas valor a valor** pelo SOPS (as chaves continuam legíveis
  em texto plano — é assim que dá pra revisar um diff de PR sem vazar o
  segredo: você vê que `JWT_KEY` mudou, não vê o valor novo).

## O que NUNCA pode ir aqui (e o `.gitignore` bloqueia)

- `*.dec.env` (qualquer `.env` decriptado).
- `secrets/*.env` sem o sufixo `.enc.env` (ex.: um `secrets/prod.env`
  plaintext acidental).
- `age.key` (a chave privada, em qualquer lugar do repo).

## Como editar um valor

Precisa ter a chave privada age real localmente (não a fictícia do
`.sops.yaml` — ver aviso abaixo) em `~/.config/sops/age/keys.txt` ou
apontada por `SOPS_AGE_KEY_FILE`.

```bash
sops secrets/prod.enc.env
```

Isso abre o arquivo **decriptado temporariamente** no seu `$EDITOR`; ao
salvar e fechar, o SOPS re-criptografa automaticamente e sobrescreve
`secrets/prod.enc.env` no disco. Nunca fica um arquivo plaintext residual.

## Como decriptar para inspecionar (sem editar)

```bash
sops --decrypt secrets/prod.enc.env
# ou, gerando um .env local temporário (NUNCA commitar, .gitignore bloqueia *.dec.env):
sops --decrypt secrets/prod.enc.env > secrets/prod.dec.env
```

## Como decriptar no deploy (droplet)

O job de deploy/`sync-config.sh` (plano 22) leva `secrets/prod.enc.env` até
a droplet; lá, com a chave privada real já instalada via cloud-init
(`infra/terraform/cloud-init.yaml`, variável `age_private_key`):

```bash
sops --decrypt secrets/prod.enc.env > .env
chmod 600 .env
```

## Rotação de segredos (JWT/DB/Email/NewRelic)

1. Trocar o valor real na fonte (ex. gerar nova `JWT_KEY`).
2. `sops secrets/prod.enc.env` → editar o valor → salvar.
3. Commitar o diff (`secrets/prod.enc.env` muda, o resto do repo não).
4. No próximo deploy, a droplet decripta a versão nova e reinicia a API
   com o valor novo (a aplicação do `.env` novo depende do `api` ser
   recriado — não é hot-reload).

## Rotação da própria chave age (trocar quem pode decriptar)

Se a chave privada precisar ser trocada (ex. suspeita de exposição):

1. Gerar um novo par: `age-keygen -o age.new.key`.
2. Atualizar `.sops.yaml` com a nova chave pública.
3. Re-encriptar todos os arquivos existentes para a nova chave:
   ```bash
   sops updatekeys secrets/prod.enc.env
   ```
   (`updatekeys` decripta com a chave antiga — que ainda precisa estar
   disponível localmente nesse momento — e re-criptografa para a(s)
   chave(s) atual(is) do `.sops.yaml`.)
4. Atualizar o GitHub Actions secret `SOPS_AGE_KEY` com a chave privada
   nova.
5. Atualizar a droplet (`~/.config/sops/age/keys.txt`) com a chave privada
   nova — manualmente via SSH, ou re-provisionando via Terraform
   (`age_private_key`/`age_public_key`) seguido de um `terraform apply`
   (cuidado: isso recria recursos dependentes do `user_data`, ver
   `docs/runbooks/secrets.md`).
6. Revogar/descartar a chave privada antiga (ela não decripta mais nada
   útil depois do passo 3, mas convém apagá-la de onde estiver guardada).

## !!! Aviso sobre a chave usada hoje no `.sops.yaml` e em `prod.enc.env` !!!

A chave pública `age16j2e2rvz65s6ppwunnxpmmmn8cfw8cpydphhlxw726d5tuxmpptsqcqf08`
referenciada em `.sops.yaml`, e usada para criptografar o `prod.enc.env`
atualmente no repo, é **fictícia** — gerada offline, apenas para validar
ponta a ponta que o fluxo `sops --encrypt`/`sops --decrypt` funciona antes
de existir uma chave real de produção. A chave privada correspondente
**nunca foi commitada em lugar nenhum e foi descartada** após a validação.

Todos os valores dentro de `prod.enc.env` hoje são placeholders
(`REPLACE_ME_...`) — **não há nenhum segredo real criptografado neste
arquivo no momento**. Antes do primeiro deploy real de produção usando
este mecanismo, é obrigatório:

1. Gerar a chave age REAL (`age-keygen -o age.key`, localmente).
2. Substituir a chave pública em `.sops.yaml`.
3. Rodar `sops secrets/prod.enc.env` (com a chave real) e preencher os
   valores reais (senha do Postgres, `JWT_KEY` real, credenciais de
   e-mail, `NR_LICENSE_KEY` real).
4. Guardar a chave privada real no GitHub secret `SOPS_AGE_KEY` e na
   droplet — nunca no repo.

Ver `docs/runbooks/secrets.md` para o passo a passo completo.
