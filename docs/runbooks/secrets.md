# Runbook — Production secrets (SOPS + age)

**English** · [Português](./secrets.pt-BR.md)

**Audience:** any human with access to the repository and to the production
droplet, even without prior context on the project.

## 1. Why this exists

Before this runbook, the production `.env` lived only on the droplet, in
plain text, with no history, no review, and no versioned backup. Now the
**backend**'s production secrets (DB, JWT, email, New Relic) live in
`secrets/prod.enc.env`, **encrypted** with [SOPS](https://github.com/getsops/sops)
using [age](https://github.com/FiloSottile/age), versioned in git like any
other file — reviewable in a PR (the keys stay readable, only the values
are encrypted), but unreadable without the private age key.

**Out of scope:** Admin Panel (static SPA, public config) and the
institutional site (`NEXT_PUBLIC_*`, public by design) have no real
server-side secrets — they don't use SOPS.

## 2. Where each piece lives

| Piece | Location | Who uses it |
|---|---|---|
| `.sops.yaml` | repo root (versioned) | `sops` automatically resolves which key to use to encrypt `secrets/*.enc.env` |
| `secrets/prod.enc.env` | repo root, `secrets/` folder (versioned, **encrypted**) | anyone can read the file in git; only whoever has the private key can decrypt the values |
| age public key | inside `.sops.yaml` (versioned) | not a secret — it's just "who to encrypt for" |
| age private key | **NEVER in git.** GitHub Actions secret `SOPS_AGE_KEY` + droplet at `~/.config/sops/age/keys.txt` | CI/CD (if it needs to decrypt in a pipeline) and the droplet (decrypts on deploy) |

## 3. Generate the real age key (one time, locally)

```bash
age-keygen -o age.key
```

Expected output:
```
Public key: age1...
```

- The **public key** (`age1...`) goes into `.sops.yaml` (replacing the
  fictitious one currently there — see §6).
- The **private key** (the full contents of the `age.key` file, including
  the `# created:`/`# public key:` comment) goes in two places, **never in
  git**:
  1. GitHub Actions secret `SOPS_AGE_KEY` (Settings → Secrets and
     variables → Actions → New repository secret).
  2. `~/.config/sops/age/keys.txt` on the droplet (permission `600`, owner
     `deploy`) — today injected automatically via Terraform/cloud-init
     (`infra/terraform/cloud-init.yaml`, variable `age_private_key` in
     `infra/terraform/variables.tf`), so normally you only need to pass the
     content as `TF_VAR_age_private_key` (or in `terraform.tfvars`, never
     committed) before `terraform apply`.
- Delete `age.key` from local disk after storing the private key in the
  two places above (or keep it in a personal password vault — never in a
  synced/versioned folder).

## 4. Edit an existing secret

Prerequisite: the real private key needs to be accessible locally
(`~/.config/sops/age/keys.txt` or `SOPS_AGE_KEY_FILE=/path/to/age.key`).

```bash
sops secrets/prod.enc.env
```

Opens the decrypted content in `$EDITOR`; upon saving and closing, SOPS
re-encrypts and overwrites the file on disk automatically. Commit the
change normally (`git add secrets/prod.enc.env && git commit`).

## 5. Decrypt on deploy (droplet)

The deploy job/`sync-config.sh` (plan 22) sends `secrets/prod.enc.env` to
the droplet. There, with the private key already installed:

```bash
sops --decrypt secrets/prod.enc.env > .env
chmod 600 .env
```

**Never** log the decrypted content in any CI/CD step (mask values with
`::add-mask::` if you need to handle them programmatically, and never use
`set -x`/`cat .env` in a step without `continue-on-error: false` explicitly
reviewed).

## 6. Replace the current fictitious key with a real one (first time)

Today (as of this commit), `.sops.yaml` and `secrets/prod.enc.env` use a
**fictitious** age key, generated only to validate the mechanism (see the
warning in `secrets/README.md`). Before the first real deploy using this
flow:

1. Generate the real key (§3).
2. Edit `.sops.yaml`, replacing the fictitious public key with the real
   one.
3. Run `sops updatekeys secrets/prod.enc.env` — **this only works if you
   still have the fictitious private key available locally** at the
   moment of the swap (`updatekeys` decrypts with the old key and
   re-encrypts for the new one(s) in `.sops.yaml`). If the fictitious key
   has already been discarded, the simpler path is: running
   `sops --decrypt` manually won't work (key already discarded, exactly as
   it should) — in that case, delete `secrets/prod.enc.env` and recreate it
   from scratch with `sops --encrypt` from a draft filled with the real
   values (see `secrets/README.md`).
4. Fill in the real values (`sops secrets/prod.enc.env`, see §4): real
   Postgres password, real `JWT_KEY` (≥32 random chars), real SMTP,
   real `NR_LICENSE_KEY`.
5. Store the real private key in the GitHub secret `SOPS_AGE_KEY` and on
   the droplet (§3).

## 7. Periodic rotation of application secrets (JWT/DB/Email/NR)

Recommendation: review/rotate every 90 days or after any suspicion of
exposure (e.g. accidental log, compromised laptop, someone with access
leaving).

1. Generate the new value at the source (e.g. new Postgres password in
   Postgres itself, new random `JWT_KEY`).
2. `sops secrets/prod.enc.env` → update the value → save → commit.
3. On the next deploy, the droplet decrypts the new version; restart the
   `api` service (it's not hot-reload — the `.env` is only read at
   container startup).
4. **Watch out with `JWT_KEY`:** rotating it invalidates every JWT token
   already issued — logged-in users will need to log in again. If the
   rotation is not due to suspected compromise, schedule it for a
   low-traffic time.

## 8. Rotating the age key itself

See `secrets/README.md`, section "Rotating the age key itself" — same
runbook, kept there to stay close to the `.sops.yaml`/`secrets/` it
directly references.

## 9. Manual prerequisites (before using this flow in production)

- [ ] Generate the real age key (`age-keygen`) **locally, on a trusted
      machine**.
- [ ] Store the real private key in `SOPS_AGE_KEY` (GitHub) and on the
      droplet.
- [ ] Update `.sops.yaml` with the real public key.
- [ ] Fill in `secrets/prod.enc.env` with the real production values
      (Postgres password, `JWT_KEY`, SMTP credentials, `NR_LICENSE_KEY`).
- [ ] Confirm that no legacy plaintext `.env` remains on the droplet
      outside the `sops --decrypt > .env` flow (remove any old manual
      copy).
