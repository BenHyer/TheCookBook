# Kubernetes manifests (`raw yaml + kubectl apply`)

These manifests deploy:
- `Cookbook.ApiService` as `cookbook-api`
- `CookbookMauiBlazor.Web` as `cookbook-web`
- NGINX `Ingress` routing on two hosts:

| Host | Path | Service |
|------|------|---------|
| `cookbook.bencampus.duckdns.org` | `/api` | `cookbook-api` |
| `cookbook.bencampus.duckdns.org` | `/grafana` | `grafana` |
| `cookbook.bencampus.duckdns.org` | `/` | `cookbook-web` |
| `cookbook.benhhome.duckdns.org` | `/api` | `cookbook-api` |
| `cookbook.benhhome.duckdns.org` | `/grafana` | `grafana` |
| `cookbook.benhhome.duckdns.org` | `/` | `cookbook-web` |

## 1) Build and push images

From repo root:

```bash
docker build -t ghcr.io/your-org/cookbook-api:latest -f Cookbook.ApiService/Dockerfile .
docker build -t ghcr.io/your-org/cookbook-web:latest -f CookbookMauiBlazor/CookbookMauiBlazor.Web/Dockerfile .

docker push ghcr.io/your-org/cookbook-api:latest
docker push ghcr.io/your-org/cookbook-web:latest
```

Update image names/tags in:
- `k8s/api-deployment.yaml`
- `k8s/web-deployment.yaml`

## 2) Set runtime configuration

Edit:
- `k8s/secret.yaml` -> `ConnectionStrings__sqldb`, `AZURE_AD_TENANT_ID`, `AZURE_AD_CLIENT_ID`, `AZURE_AD_CLIENT_SECRET` (not committed to git)
- `k8s/configmap.yaml` -> `ApiService__BaseUrl`, `AzureAd__Instance`, and environment
- `k8s/ingress.yaml` -> `spec.rules[*].host` if adding/changing DuckDNS hostnames

## 3) Deploy

```bash
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/
```

## 4) Verify

```bash
kubectl get pods,svc,ing -n cookbook
kubectl logs deployment/cookbook-api -n cookbook
kubectl logs deployment/cookbook-web -n cookbook
```

## Notes

- Requires an ingress controller installed in the cluster (for example NGINX ingress).
- If your registry is private, add an image pull secret and reference it in both deployments.
- The MAUI app project is not deployed to Kubernetes.
