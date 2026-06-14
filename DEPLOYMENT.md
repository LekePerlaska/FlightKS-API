# FlightKS — Deployment Guide

Deploys the full FlightKS stack to Kubernetes with the Helm chart in
[`helm/flightks`](helm/flightks). The chart provisions: the Next.js **frontend**,
the ASP.NET Core **backend** (API), **PostgreSQL**, **Redis**, and **Keycloak**,
exposed through a single **NGINX Ingress**. (The docker-compose
nginx edge is *not* deployed into Kubernetes — the Ingress replaces it.)

> All values, service names, ports and keys below come from the actual chart
> (`helm/flightks/values.yaml` and `helm/flightks/templates/`). Nothing here is
> invented.

---

## 1. Prerequisites

| Tool | Purpose | Check |
|------|---------|-------|
| **Docker** | Build/push container images (and run Docker Desktop's local K8s) | `docker version` |
| **Kubernetes cluster** | Target for the deployment (Docker Desktop, kind, or a remote cluster) | `kubectl get nodes` |
| **kubectl** | Cluster CLI | `kubectl version --client` |
| **Helm** (v3 or v4) | Install/manage the chart | `helm version` |
| **NGINX Ingress Controller** | Serves the single public origin (the chart's Ingress uses `ingressClassName: nginx`) | `kubectl get pods -n ingress-nginx` |

Cluster requirements the chart relies on:

- A **default StorageClass** — PostgreSQL uses a `volumeClaimTemplates` PVC (`data-flightks-postgres-0`).
- The **NGINX Ingress Controller** must already be installed. If it isn't:
  ```bash
  helm upgrade --install ingress-nginx ingress-nginx/ingress-nginx \
    -n ingress-nginx --create-namespace
  # repo: helm repo add ingress-nginx https://kubernetes.github.io/ingress-nginx
  ```
- For **HTTPS** (required for login — see §7), **cert-manager** must be installed
  in the cluster.

---

## 2. Connect to the cluster with your kubeconfig

The kubeconfig file (e.g. `project-09-kubeconfig.yaml`) holds live cluster
credentials. **It is gitignored (`*kubeconfig*.yaml`) — never commit it.**

Use it per-command via the `KUBECONFIG` env var (does not touch your default
config):

```bash
export KUBECONFIG=/path/to/project-09-kubeconfig.yaml

kubectl config current-context        # e.g. project-09
kubectl get nodes                     # confirm reachable
kubectl config view --minify -o jsonpath='{..namespace}{"\n"}'   # default namespace
```

Or pass it inline to a single command:

```bash
kubectl --kubeconfig=/path/to/project-09-kubeconfig.yaml get pods -n project-09
```

> A namespaced ServiceAccount kubeconfig can typically create workloads/services/
> ingresses/PVCs/secrets **inside its own namespace** but cannot list/modify
> cluster-scoped objects (StorageClasses, ClusterIssuers, IngressClasses). That is
> fine — the chart only needs namespaced resources.

---

## 3. Build and push GHCR images

The chart pulls images from GHCR (defaults in `values.yaml`):

| Component | Default image | Notes |
|-----------|---------------|-------|
| backend | `ghcr.io/lekeperlaska/flightks-api` | Built from the repo root `Dockerfile`. CI (`.github/workflows/ci.yml`) builds & pushes on push to `master`/`staging`/`development`. |
| frontend | `ghcr.io/valezasutaj/flightks-client` | Built from the frontend repo's `Dockerfile`. |

### Backend — build locally and push

The host SDK is .NET 8 but the project targets .NET 10, so the build happens
**inside the image** (the `Dockerfile` uses the SDK 10 base) — you only need
Docker:

```bash
# from the repo root
docker build -t ghcr.io/<owner>/flightks-api:<tag> -f Dockerfile .

# push (PAT needs the write:packages scope)
echo <GHCR_PAT> | docker login ghcr.io -u <github-user> --password-stdin
docker push ghcr.io/<owner>/flightks-api:<tag>
docker logout ghcr.io
```

Then point the chart at it: `--set backend.image.repository=ghcr.io/<owner>/flightks-api --set backend.image.tag=<tag>`.

### Via CI

Push to `master`, `staging`, or `development` (or a `v*` tag) and the
`docker-build` job builds `ghcr.io/${repo}` with tags `latest` (default branch),
the commit SHA, and semver tags. After CI finishes, redeploy (see §8) and force a
pull if you reused `:latest` (`kubectl rollout restart deploy/flightks-backend`).

### Image pull secret (for private GHCR packages)

The chart references a pull secret named in `image.pullSecrets` (default
`ghcr-creds`). Create it from a PAT with `read:packages`:

```bash
kubectl create secret docker-registry ghcr-creds \
  --docker-server=ghcr.io \
  --docker-username=<github-user> \
  --docker-password=<PAT-with-read:packages> \
  -n <namespace>
```

> If your images are **public**, the pull secret is harmless (anonymous pull
> still works). It is required only for **private** packages.

---

## 4. Create / update Kubernetes Secrets safely

The chart can render the Secret for you (`secrets.create: true`, default), but
**never commit real secret values** — supply them at install time. The Secret is
named `flightks-secrets` and holds these keys:

| Secret key | From value | Used by |
|------------|-----------|---------|
| `postgres-user` / `postgres-password` | `secrets.postgresUser` / `…Password` | backend conn string, Keycloak DB, Postgres |
| `redis-password` | `secrets.redisPassword` | backend rate-limit store, Redis |
| `keycloak-admin-user` / `keycloak-admin-password` | `secrets.keycloakAdminUser` / `…Password` | Keycloak bootstrap admin |
| `keycloak-admin-client-secret` | `secrets.keycloakAdminClientSecret` | backend → Keycloak admin API |

**Recommended ways to supply real values:**

```bash
# A) inline at install (good for a few values)
helm upgrade --install flightks ./helm/flightks -n <ns> \
  --set secrets.postgresPassword='…' \
  --set secrets.redisPassword='…' \
  --set secrets.keycloakAdminPassword='…' \
  --set secrets.keycloakAdminClientSecret='…'

# B) a private values file (gitignored), passed with -f
helm upgrade --install flightks ./helm/flightks -n <ns> -f my-secrets.yaml
```

**Bring your own Secret** (e.g. SealedSecrets / external-secrets): set
`--set secrets.create=false` and pre-create a Secret named `flightks-secrets`
with the keys above.

> The full DB connection string is **not** stored as a secret value — the backend
> assembles `ConnectionStrings__DefaultConnection` at runtime from
> `$(POSTGRES_USER)`/`$(POSTGRES_PASSWORD)` env refs, so the password lives only
> in the Secret.

### Keycloak realm ConfigMap (required when `keycloak.realmImport.enabled=true`)

The realm is imported from the repo's existing `keycloak/realm-export.json`
**without copying it into the chart**. Create the ConfigMap first:

```bash
kubectl create configmap flightks-keycloak-realm \
  --from-file=realm-export.json=keycloak/realm-export.json \
  -n <namespace>
```

Keycloak imports it only when the realm does not already exist (skipped if the
Postgres `keycloak` database already has the realm).

---

## 5. Helm commands

All run from the repo root; the chart is `./helm/flightks`.

### Lint
```bash
helm lint ./helm/flightks
```

### Render (dry-run the YAML without applying)
```bash
helm template flightks ./helm/flightks \
  --set ingress.host=flightks.local

# preview against the cluster API (catches version/CRD issues):
helm upgrade --install flightks ./helm/flightks -n <ns> --dry-run --debug
```

### Install / upgrade
```bash
# 1) ensure the realm ConfigMap and (if private images) ghcr-creds exist in <ns>
# 2) then:
helm upgrade --install flightks ./helm/flightks \
  -n <ns> --create-namespace \
  --set ingress.host=<your-host> \
  --set secrets.postgresPassword='…' \
  --set secrets.redisPassword='…' \
  --set secrets.keycloakAdminPassword='…' \
  --set secrets.keycloakAdminClientSecret='…'
```

Namespace handling:
- All resources are created in the **release namespace** — choose it with
  `-n <ns>`. The chart does not manage the Namespace object itself.
- **New namespace**: add `--create-namespace` and Helm creates it (e.g.
  `-n flightks --create-namespace`).
- **Existing/shared namespace** (e.g. `project-09`, where you can't create
  namespaces): just `-n project-09` — it already exists, so drop `--create-namespace`.

Re-running to change one thing while keeping previous values:
```bash
helm upgrade flightks ./helm/flightks -n <ns> --reuse-values \
  --set backend.image.tag=<new-tag>
```

### HTTPS (cert-manager) — required for login (see §7)
```bash
# 1) create a namespaced ACME Issuer (Issuers are namespaced; no cluster perms needed)
#    spec.acme.solvers[].http01.ingress.ingressClassName: nginx
kubectl apply -f issuer.yaml -n <ns>

# 2) enable TLS + reference the issuer + switch the public scheme to https
helm upgrade flightks ./helm/flightks -n <ns> --reuse-values \
  --set publicScheme=https \
  --set ingress.tls.enabled=true \
  --set ingress.tls.secretName=flightks-tls \
  --set ingress.annotations."cert-manager\.io/issuer"=letsencrypt
```
This flips `Keycloak:Authority`, CORS, and `KC_HOSTNAME` to `https://<host>` and
adds the TLS block + annotation so cert-manager issues `flightks-tls`.
The GitHub Actions deployment workflow applies the same HTTPS settings for the
`project-09` namespace on default-branch deploys.

### Uninstall
```bash
helm uninstall flightks -n <ns>
```
(See §9 for a *complete* cleanup including PVCs.)

---

## 6. kubectl commands (observe the deployment)

```bash
NS=<namespace>   # e.g. flightks or project-09

kubectl get pods -n $NS                 # all pods + status
kubectl get svc  -n $NS                 # services + ClusterIPs
kubectl get ingress -n $NS              # ingress host + ADDRESS
kubectl describe pod <pod-name> -n $NS  # events, probe failures, pull errors
kubectl logs <pod-name> -n $NS          # container logs
kubectl logs -n $NS -l app.kubernetes.io/component=backend --tail=100   # by component
```

Components are labelled `app.kubernetes.io/component=` `backend` | `frontend` |
postgres | edis | keycloak.

Services created by the chart (release `flightks`):

| Service | Type | Port |
|---------|------|------|
| `flightks-frontend` | ClusterIP | 3000 |
| `flightks-backend` | ClusterIP | 5194 |
| `flightks-keycloak` | ClusterIP | 8080 |
| `flightks-redis` | ClusterIP | 6379 |
| `flightks-postgres` | Headless | 5432 |

---

## 7. Verification

Set `HOST` to your ingress host (e.g. `flightks.local`, or `flightks.<ip>.nip.io`),
and use `https://` once TLS is enabled.

### Frontend
```bash
curl -s -o /dev/null -w "%{http_code}\n" http://$HOST/        # expect 200
```

### Backend (API base path is /api/v1)
```bash
curl -s -o /dev/null -w "%{http_code}\n" http://$HOST/api/v1/auth/me   # expect 401 (no token)
```
A clean **401** (not 500) confirms the API is up and JWT auth is wired.

### `/health`
`/health` is the readiness/liveness probe path on the **backend container
(port 5194)** and is **not exposed through the Ingress** (the Ingress only routes
`/api`, `/hubs`, `/realms`, `/resources`, `/js`, and `/`). Check it in-cluster:
```bash
kubectl port-forward svc/flightks-backend 5194:5194 -n $NS
curl -s http://localhost:5194/health        # expect 200
```
(Or rely on `kubectl get pods` showing the backend `READY 1/1`, which means the
`/health` probe is passing.)

### Keycloak
```bash
curl -s -o /dev/null -w "%{http_code}\n" http://$HOST/realms/flightks   # expect 200
curl -s http://$HOST/realms/flightks/.well-known/openid-configuration | \
  grep -o '"issuer":"[^"]*"'      # issuer must equal http(s)://$HOST/realms/flightks
```

**Login requires HTTPS.** The frontend uses keycloak-js with PKCE (`S256`),
which needs the browser **Web Crypto API** — only available over **HTTPS** or
`http://localhost`. Over plain HTTP on a public host the Sign-In button throws
`Web Crypto API is not available`. So for any non-localhost host, complete the
HTTPS step in §5 before expecting login to work.

For login to succeed, the realm's `flightks-client` must also allow your host:
its **redirect URIs** must include `http(s)://$HOST/*` and **web origins** must
include `http(s)://$HOST`, and the realm's `sslRequired` must match your scheme
(`external` for HTTPS; `none` only if you intentionally serve plain HTTP). Manage
these in the Keycloak admin console (see below) or via the admin REST API.

Reaching the **Keycloak admin console**: `/admin` on the Ingress routes to the
frontend (by design), so port-forward Keycloak and set its admin URL:
```bash
helm upgrade flightks ./helm/flightks -n $NS --reuse-values \
  --set keycloak.adminUrl=http://localhost:8088
kubectl port-forward svc/flightks-keycloak 8088:8080 -n $NS
# open http://localhost:8088  (login: keycloak-admin-user / keycloak-admin-password)
```


### SignalR / WebSockets
SignalR hubs are served under `/hubs` (e.g. `/hubs/seats`,
`/hubs/notifications`, `/hubs/admin-dashboard`) and routed to the backend by the
Ingress, which carries long `proxy-read-timeout`/`proxy-send-timeout` (3600s) so
connections aren't torn down. ingress-nginx performs the WebSocket upgrade
automatically. Verify the negotiate handshake (needs an `access_token` query
param for authenticated hubs):
```bash
curl -s -o /dev/null -w "%{http_code}\n" \
  -X POST "http://$HOST/hubs/notifications/negotiate?negotiateVersion=1&access_token=<JWT>"
# 200 = hub reachable through the ingress
```
In the app, real-time features (seat map, notifications, admin dashboard)
updating live is the end-to-end confirmation.

---

## 8. Troubleshooting

Start with `kubectl get pods -n $NS` and `kubectl describe pod <pod> -n $NS`
(Events section) for almost everything below.

### `ImagePullBackOff` / `ErrImagePull`
- Private GHCR image without a valid pull secret. Ensure `ghcr-creds` exists in
  `$NS` (see §3) and that `image.pullSecrets` includes it (default does).
- Wrong repo/tag: confirm `backend.image.*` / `frontend.image.*` point at an
  image that actually exists. `kubectl describe pod` shows the exact image+error.
- Token lacks `read:packages` for that package, or the package is private and the
  token is for a different account.

### `CrashLoopBackOff`
- `kubectl logs <pod> -n $NS` (and `--previous` for the last crash).
- **Backend** runs EF Core `MigrateAsync` on startup — if Postgres isn't ready
  yet it crashes and restarts until Postgres is up (self-heals). Persistent
  crashes usually mean a bad connection string or unreachable dependency (below).
- **Keycloak** first boot does DB migration + (optionally) realm import — give it
  time; the `startupProbe` allows a long boot.

### Failed readiness/liveness probes
- **Backend**: probe is `GET /health` on port 5194. A failing probe means the app
  isn't serving — check logs for startup/migration/dependency errors.
- **Keycloak**: probes hit the **management port 9000** (`/health/started`,
  `/health/ready`, `/health/live`); `KC_HEALTH_ENABLED=true` is set by the chart.
- **Postgres**: `pg_isready`.
- **Redis**: TCP probe on 6379.
- `kubectl describe pod` shows which probe failed and the message.

### Backend cannot connect to PostgreSQL
- Backend connects to host `flightks-postgres:5432`, database `flightks`, using
  `$(POSTGRES_USER)/$(POSTGRES_PASSWORD)` from `flightks-secrets`.
- Confirm Postgres is `READY 1/1` and the user/password in the Secret match what
  Postgres was initialised with. If you changed `secrets.postgres*` after the PVC
  was created, the old credentials persist in the volume — reset needs a fresh PVC.
- Keycloak uses the **same** Postgres instance, database `keycloak` (created by
  the first-boot init ConfigMap `flightks-postgres-init`). If Keycloak can't find
  its DB, the init script didn't run (PVC wasn't empty on first boot).

### Backend cannot connect to Redis
- Backend uses `RateLimiting__RedisConnectionString = flightks-redis:6379,password=$(REDIS_PASSWORD)`
  with `RateLimiting__Store=Distributed`.
- Ensure the `redis-password` Secret value matches Redis's `--requirepass`
  (both come from `secrets.redisPassword`, so they match unless you edited one).

### Backend cannot connect to Keycloak
- The backend fetches OIDC metadata internally at
  `Keycloak__MetadataAddress = http://flightks-keycloak:8080/realms/flightks/.well-known/openid-configuration`
  and uses `Keycloak__InternalAuthority` for token/logout calls — these stay on
  the in-cluster service even when the public `Authority` is `https://$HOST`.
- Token **audience**/issuer mismatch → 401 on `/api/v1/...` with a real token:
  the public `Keycloak:Authority` must equal the token issuer (`http(s)://$HOST/realms/flightks`),
  and `Keycloak:Audience` (`flightks-api`) must be present in the token (realm
  audience mapper).
- Admin features (user/role management) 500 → the backend's
  `keycloak-admin-client-secret` must match the realm's `flightks-admin-client`
  secret, and that client must have the realm-management roles.


### Ingress routing issues
- `kubectl get ingress -n $NS` must show an `ADDRESS`. No address → the NGINX
  Ingress Controller isn't installed or `ingressClassName` doesn't match.
- Routing is path-based: `/api`, `/hubs` → backend; `/realms`, `/resources`,
  `/js` → keycloak; `/` (everything else, including the SPA's `/admin` and
  `/auth/*` pages) → frontend. If a frontend route returns Keycloak's "We are
  sorry / page not found", a path is being mis-routed to Keycloak.
- 404 on `/health` via the host is expected — it's not an Ingress route (see §7).

### WebSocket issues
- Hubs hanging/closing: confirm the Ingress annotations
  `nginx.ingress.kubernetes.io/proxy-read-timeout` / `proxy-send-timeout` are
  present (the chart sets 3600s) — short timeouts drop idle hub connections.
- Authenticated hubs need the token in the `access_token` query parameter (the
  backend reads it there for `/hubs`), not an `Authorization` header.
- 502/504 on `/hubs/*/negotiate`: the backend isn't ready, or the path isn't
  routed to `flightks-backend`.

---

## 9. Clean uninstall

`helm uninstall` removes the chart's resources but **StatefulSet PVCs and any
objects you created outside the chart persist**. Full cleanup:

```bash
NS=<namespace>

# 1) remove the Helm release (Deployments, Services, Ingress, ConfigMap, Secret, StatefulSets)
helm uninstall flightks -n $NS

# 2) delete the StatefulSet PVCs (data is intentionally retained by K8s)
kubectl delete pvc data-flightks-postgres-0 -n $NS

# 3) objects created manually (only if you added them):
kubectl delete configmap flightks-keycloak-realm -n $NS      # realm import source
kubectl delete secret ghcr-creds -n $NS                      # image pull secret
kubectl delete secret flightks-tls -n $NS                    # TLS cert (if HTTPS)
kubectl delete issuer letsencrypt -n $NS                     # cert-manager Issuer (if HTTPS)

# 4) if you created the namespace for this app (e.g. via --create-namespace) and want it gone:
kubectl delete namespace $NS
```

> Deleting the PVCs erases all Postgres (app + Keycloak realm) data.
> Skip step 2 to preserve data across a reinstall.

---

## Production hardening (chart defaults)

The chart ships production defaults out of the box (all configurable in `values.yaml`):

- **Non-root, least-privilege pods** — the app tier (backend/frontend/keycloak)
  runs with `runAsNonRoot`, `allowPrivilegeEscalation: false`, all Linux
  capabilities dropped, and `seccompProfile: RuntimeDefault`. (The data tier keeps
  its image defaults — Postgres must start as root to fix volume ownership.)
- **Dedicated ServiceAccount with no API token** — every pod uses it with
  `automountServiceAccountToken: false` (nothing here calls the K8s API).
- **Zero-downtime rollouts** — backend/frontend use `RollingUpdate` with
  `maxUnavailable: 0`.
- **PodDisruptionBudgets** — keep ≥1 backend/frontend replica during node drains.
- **Values schema** (`values.schema.json`) — bad/missing values fail fast at
  install time.

## Reference: chart layout

```
helm/flightks/
├── Chart.yaml
├── values.yaml
├── values.schema.json          # validates values on install/upgrade
├── .helmignore
└── templates/
    ├── _helpers.tpl             # names, labels, namespace, URLs, pull-secrets, SA name
    ├── NOTES.txt                # post-install instructions
    ├── serviceaccount.yaml      # dedicated SA, token not mounted
    ├── configmap.yaml           # non-sensitive backend config (Section__Key env)
    ├── secret.yaml              # flightks-secrets (placeholders → override at install)
    ├── backend.yaml             # Deployment + Service
    ├── frontend.yaml            # Deployment + Service
    ├── postgres.yaml            # init ConfigMap (creates keycloak DB) + StatefulSet + Service
    ├── redis.yaml               # Deployment + Service
    ├── keycloak.yaml            # Deployment + Service
    ├── pdb.yaml                 # PodDisruptionBudgets (backend, frontend)
    └── ingress.yaml
```
Each component's Deployment/StatefulSet and its Service live in one file. The
namespace isn't a template — resources land in the release namespace (`helm -n`).
The Keycloak **realm ConfigMap** and the cert-manager **Issuer** are created with
`kubectl` (not chart templates) — see §4 and §5.
