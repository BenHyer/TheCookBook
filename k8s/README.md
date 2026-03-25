# Kubernetes manifests (`raw yaml + kubectl apply`)

These manifests deploy:
- `Cookbook.ApiService` as `cookbook-api`
- `CookbookMauiBlazor.Web` as `cookbook-web`
- NGINX `Ingress` routing:
  - `/api` -> `cookbook-api`
  - `/` -> `cookbook-web`

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
- `k8s/secret.yaml` -> `ConnectionStrings__sqldb`
- `k8s/configmap.yaml` -> `ApiService__BaseUrl` and environment
- `k8s/ingress.yaml` -> `spec.rules[0].host` (currently `cookbook.local`)

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
