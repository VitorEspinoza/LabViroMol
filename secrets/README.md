# secrets/ — encrypted production secrets (SOPS + age)

**English** · [Português](./README.pt-BR.md)

This folder versions the backend's production secrets, **encrypted** with
[SOPS](https://github.com/getsops/sops) using [age](https://github.com/FiloSottile/age)
as the encryption backend. The full runbook is at
[`docs/runbooks/secrets.md`](../docs/runbooks/secrets.md).

## What can go here

- `prod.enc.env` — the only file today. Backend production environment
  variables (DB, JWT, Email, observability), in `dotenv` format,
  **encrypted value by value** by SOPS (the keys stay readable in plain
  text — that's what lets you review a PR diff without leaking the
  secret: you see that `JWT_KEY` changed, not the new value).

## What must NEVER go here (and `.gitignore` blocks)

- `*.dec.env` (any decrypted `.env`).
- `secrets/*.env` without the `.enc.env` suffix (e.g. an accidental
  plaintext `secrets/prod.env`).
- `age.key` (the private key, anywhere in the repo).

## How to edit a value

You need the real age private key locally (not the fictitious one from
`.sops.yaml` — see the warning below) at `~/.config/sops/age/keys.txt` or
pointed to by `SOPS_AGE_KEY_FILE`.

```bash
sops secrets/prod.enc.env
```

This opens the file **temporarily decrypted** in your `$EDITOR`; upon
saving and closing, SOPS automatically re-encrypts and overwrites
`secrets/prod.enc.env` on disk. No residual plaintext file is ever left
behind.

## How to decrypt for inspection (without editing)

```bash
sops --decrypt secrets/prod.enc.env
# or, generating a temporary local .env (NEVER commit it, .gitignore blocks *.dec.env):
sops --decrypt secrets/prod.enc.env > secrets/prod.dec.env
```

## How to decrypt on deploy (droplet)

The deploy job/`sync-config.sh` (plan 22) carries `secrets/prod.enc.env`
to the droplet; there, with the real private key already installed via
cloud-init (`infra/terraform/cloud-init.yaml`, variable
`age_private_key`):

```bash
sops --decrypt secrets/prod.enc.env > .env
chmod 600 .env
```

## Secret rotation (JWT/DB/Email/NewRelic)

1. Change the real value at the source (e.g. generate a new `JWT_KEY`).
2. `sops secrets/prod.enc.env` → edit the value → save.
3. Commit the diff (`secrets/prod.enc.env` changes, nothing else in the
   repo does).
4. On the next deploy, the droplet decrypts the new version and restarts
   the API with the new value (applying the new `.env` depends on the
   `api` being recreated — it's not hot-reload).

## Rotating the age key itself (changing who can decrypt)

If the private key needs to be replaced (e.g. suspected exposure):

1. Generate a new pair: `age-keygen -o age.new.key`.
2. Update `.sops.yaml` with the new public key.
3. Re-encrypt all existing files for the new key:
   ```bash
   sops updatekeys secrets/prod.enc.env
   ```
   (`updatekeys` decrypts with the old key — which still needs to be
   available locally at this moment — and re-encrypts for the current
   key(s) in `.sops.yaml`.)
4. Update the GitHub Actions secret `SOPS_AGE_KEY` with the new private
   key.
5. Update the droplet (`~/.config/sops/age/keys.txt`) with the new
   private key — manually via SSH, or by re-provisioning via Terraform
   (`age_private_key`/`age_public_key`) followed by a `terraform apply`
   (careful: this recreates resources dependent on `user_data`, see
   `docs/runbooks/secrets.md`).
6. Revoke/discard the old private key (it no longer decrypts anything
   useful after step 3, but it's still worth deleting it from wherever it
   was stored).

## !!! Warning about the key currently used in `.sops.yaml` and `prod.enc.env` !!!

The public key `age16j2e2rvz65s6ppwunnxpmmmn8cfw8cpydphhlxw726d5tuxmpptsqcqf08`
referenced in `.sops.yaml`, and used to encrypt the `prod.enc.env`
currently in the repo, is **fictitious** — generated offline, solely to
validate end-to-end that the `sops --encrypt`/`sops --decrypt` flow works
before a real production key exists. The corresponding private key **was
never committed anywhere and was discarded** after validation.

All values inside `prod.enc.env` today are placeholders
(`REPLACE_ME_...`) — **there is currently no real secret encrypted in
this file**. Before the first real production deploy using this
mechanism, it is mandatory to:

1. Generate the REAL age key (`age-keygen -o age.key`, locally).
2. Replace the public key in `.sops.yaml`.
3. Run `sops secrets/prod.enc.env` (with the real key) and fill in the
   real values (Postgres password, real `JWT_KEY`, email credentials,
   real `NR_LICENSE_KEY`).
4. Store the real private key in the GitHub secret `SOPS_AGE_KEY` and on
   the droplet — never in the repo.

See `docs/runbooks/secrets.md` for the complete step-by-step guide.
