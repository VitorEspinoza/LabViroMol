# Tutorial — Como gerar, organizar e escrever sobre os relatórios de carga

[English](./REPORTING.md) · **Português**

Este documento complementa o [README.md](README.pt-BR.md) (que explica *o que* cada peça da suíte faz) com o
passo a passo de **depois que o teste rodou**: onde os números ficam, como organizá-los pra não se perder
entre campanhas, e como transformar isso em texto de TCC.

---

## 1. O que cada execução gera

Toda execução de `--command=run` cria a pasta:

```
tests/LoadTests/bin/Release/net10.0/reports/<campanha>/<perfil>/<cenário>/
```

Dentro dela, dois grupos de artefato:

| Arquivo | Quem gera | O que tem |
|---|---|---|
| `*.html` | NBomber (`WithReportFolder`) | Relatório visual com gráficos de latência/RPS por *step*, navegável no browser |
| `*.csv`, `*.md`, `*.txt` | NBomber | Os mesmos números do html em formato bruto/tabular — use o `.csv` se for plotar gráfico próprio |
| `summary.json` | Nosso `ResultExporter.cs` | Campanha/perfil/cenário, **breakdown de status code**, **Apdex por operação** e p95/p99/max por cenário — é o arquivo mais útil pra montar tabela comparativa, porque já vem achatado |

O nome da pasta (`<campanha>/<perfil>/<cenário>`) vem direto dos argumentos `--campaign`, `--profile`,
`--scenario` que você passou na linha de comando. **Rodar de novo com os mesmos três valores sobrescreve
o relatório anterior** — não existe versionamento automático por timestamp.

---

## 2. Convenção de nomes pra não perder resultado

Como o `--campaign` é só uma string livre (não precisa ser literalmente "A" ou "B"), use-o pra carimbar
qualquer variável do experimento que não seja perfil/cenário:

| Experimento | Valor de `--campaign` sugerido |
|---|---|
| Campanha A, tradução real (padrão) | `A` |
| Campanha A, ablation do LibreTranslate | `A-noop-translate` |
| Campanha B | `B` |
| Campanha B, ablation | `B-noop-translate` |
| Reexecução depois de corrigir um bug encontrado | `A-rerun-2026-06-25` (ou qualquer sufixo que te diga "isso é depois da correção X") |

Antes de começar uma campanha nova, copie a pasta `reports/` inteira pra fora do `bin/` (que é
descartável a cada build) — veja seção 3.

---

## 3. Organizando os artefatos como evidência (fora do `bin/`)

`bin/` é recriado a cada `dotnet build`/`dotnet run`, então **não é lugar de guardar evidência**. Crie uma
pasta de evidências versionada, fora do projeto de código (pra não inflar o repo com html/csv de teste):

```
evidencias-tcc/
  capitulo-testes/
    01-smoke/
    02-load-nominal/
    03-stress/
    04-soak-60min/
    05-ablation-libretranslate/
    06-campanha-b/
```

Depois de cada execução, copie a pasta de relatório pra dentro da subpasta correspondente, com um nome que
inclua data e comando usado, por exemplo:

```bash
cp -r tests/LoadTests/bin/Release/net10.0/reports/A/load/mixed \
      evidencias-tcc/capitulo-testes/02-load-nominal/2026-06-21_A_load_mixed
```

Guarde também, ao lado, um `comando.txt` com a linha exata que você rodou — você vai precisar reproduzir
isso na defesa do TCC se alguém perguntar "como você gerou esse número".

---

## 4. Montando a tabela comparativa (A × B × ablation)

O `summary.json` de cada execução já vem com os campos prontos. Pra comparar N execuções, monte uma
tabela manual (ou um script simples que leia os `summary.json` de cada pasta) com uma linha por
cenário/operação:

| Campanha | Cenário | p95 (ms) | p99 (ms) | Erro (%) | Apdex |
|---|---|---|---|---|---|
| A | read-simple | … | … | … | … |
| A-noop-translate | read-simple | … | … | … | … |
| B | read-simple | … | … | … | … |

O delta entre `A` e `A-noop-translate` na mesma linha é o custo real do LibreTranslate. O delta entre `A`
e `B` é o custo dos limites de container (384 MB / 0,5 vCPU). Essas duas comparações são o resultado mais
citável do capítulo — cada uma isola **uma única variável**, que é o que dá força de "experimento
controlado" ao invés de "rodei e deu esse número".

---

## 5. Correlacionando com o New Relic

Pra cada execução que você for citar no TCC:

1. **Anote o horário de início e fim exato** (UTC) — o NBomber loga isso no `.txt`/`.html`.
2. No New Relic, vá ao dashboard de `cqrs.duration`, ao meter do Npgsql e ao runtime (CPU/GC) e **restrinja
   a janela de tempo** para esse intervalo exato.
3. Tire print dos gráficos já recortados na janela — não do dashboard "ao vivo" (que mistura outros
   períodos). Salve junto da evidência da seção 3, mesma subpasta.
4. Se o cliente (NBomber) registrou erro 500/timeout num horário X, confira se o servidor mostra o sintoma
   correspondente (fila de conexão Npgsql, CPU 100%, GC em loop) **no mesmo X** — isso é a "prova" de causa
   e efeito que sustenta a seção de análise.

---

## 6. Como estruturar a escrita (capítulo de testes do TCC)

Sugestão de seções, na ordem em que normalmente aparecem num capítulo de avaliação experimental:

### 6.1 Metodologia (antes de mostrar qualquer número)
- Que ferramenta (NBomber) e por quê (linguagem, integração com os contratos/DTOs do próprio projeto).
- Os SLOs definidos **a priori** — a tabela de T/4T por tipo de operação (seção 2/3 do `README.md`).
  Importante deixar claro que o threshold foi decidido *antes* de rodar, não ajustado depois pra "passar".
- O ambiente: especificação da VM, limites de container de cada campanha, e por que a Campanha A isola só
  os recursos como variável (ver README seção 6).
- As variáveis de controle: mock de SMTP (por que é seguro mockar — dependência 100% externa) vs
  LibreTranslate real (por que não é seguro mockar — disputa recursos no mesmo host).

### 6.2 Execução
- Lista dos comandos exatos rodados (reaproveite os `comando.txt` da seção 3).
- Datas/horários de cada rodada.
- Qualquer desvio do plano (ex.: um teste que teve que ser refeito por bug encontrado no meio).

### 6.3 Resultados
- Uma tabela por perfil (smoke/load/stress/soak), com p95/p99/erro/Apdex por cenário.
- Os gráficos do NBomber (latência ao longo do tempo) pros perfis mais longos (soak, stress) — é aqui que
  aparece visualmente memory leak ou degradação.
- A tabela comparativa da seção 4 (A × B × ablation).

### 6.4 Análise / Discussão
- Para cada limite encontrado, a correlação cliente↔servidor da seção 5 (qual recurso saturou primeiro).
- O Apdex por faixa de carga (nominal/tolerada/ruptura) — isso é o "veredito" de qualidade de serviço.
- Comparação explícita com os SLOs definidos na metodologia: passou, passou parcialmente, ou não passou —
  e por quê.

### 6.5 Limitações
- Teste roda fora do servidor real (agente externo) mas ainda na mesma rede/região — declare isso.
- Sem usuários reais simultâneos validando comportamento (é carga sintética).
- Sampling de trace reduzido (0,1) durante o teste — alguns traces individuais não ficaram disponíveis pra
  inspeção pós-morte, só os agregados.

### 6.6 Conclusão
- O número de referência prático: "a API sustenta X RPS com Apdex ≥ 0,90 sob os limites de produção atuais
  (384 MB / 0,5 vCPU)".
- O gargalo identificado (RAM/GC, pool, ou CPU do nginx) e a recomendação decorrente (ex.: "se o tráfego
  real superar X, o primeiro recurso a aumentar é Y").

---

## 7. Checklist antes de considerar um resultado "fechado"

- [ ] O comando exato foi salvo (`comando.txt`) junto da evidência.
- [ ] `summary.json` confirma se passou ou não nos thresholds definidos a priori.
- [ ] Print do New Relic recortado na janela de tempo exata da execução.
- [ ] Se for um resultado citado em tabela comparativa (A×B ou ablation), a outra ponta da comparação já
      foi rodada e está na mesma estrutura de pastas.
- [ ] Rodada de soak (se aplicável) não mostra crescimento monotônico de latência/heap no gráfico do
      NBomber — ou, se mostra, isso foi anotado como achado (não escondido).
