# READ ME
Author: WillyPain
Date: 25/04/2025

### Dev Build Steps
- Create self signed cert
	- openssl
- Create Client Secrets for following projects
	- TIP: can use following code to generate secret `Convert.ToHexString(RandomNumberGenerator.GetBytes(32))`
	- Identity.Server
		-  "ClientSecrets:MatchMaking.Api": <secret here>
		-  "ClientSecrets:GameServerOrchestrator": <secret here>
	- MatchMaking.Api
		- "OAuth:ClientSecret": <secret here>
	- GameServerOrchestrator
		- "OAuth:ClientSecret": <secret here>

- Add secrets to secrets.k8s.local.yaml as BASE 64
	- TIP: Can use following code to encode secret `Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(<secret>))`
    - `data:
	     matchmaking-api: <base 64 encoded secret here>
         gso: <base 64 encoded secret here>`
- Build dev images
    - Run `docker compose build`
- Start K8s kind from docker desktop
- Install nginx ingress controller
	- helm repo add ingress-nginx https://kubernetes.github.io/ingress-nginx 
	- helm repo update
	- `helm install ingress-nginx ingress-nginx/ingress-nginx \
	  --namespace ingress-nginx --create-namespace \
	  --set controller.extraArgs.default-ssl-certificate=ingress-nginx/susine.dev`
- Create secret for dev cert
	- kubectl create secret tls susine.dev --key susine.dev.key --cert susine.dev.crt -n ingress-nginx
- Install Linkerd Service Mesh (With Helm)
	- helm repo add linkerd-edge https://helm.linkerd.io/edge
	- helm install linkerd-crds linkerd-edge/linkerd-crds -n linkerd --create-namespace
	- step certificate create root.linkerd.cluster.local ca.crt ca.key --profile root-ca --no-password --insecure
	- step certificate create identity.linkerd.cluster.local issuer.crt issuer.key --profile intermediate-ca --not-after 8760h --no-password --insecure --ca ca.crt --ca-key ca.key
	- helm install linkerd-control-plane -n linkerd --set-file identityTrustAnchorsPEM=ca.crt --set-file identity.issuer.tls.crtPEM=issuer.crt --set-file identity.issuer.tls.keyPEM=issuer.key linkerd-edge/linkerd-control-plane
- Apply the following k8s manifest files in Docker folder
	- kubectl apply -f identity.k8s.local.yaml
	- kubectl apply -f matchmaking.k8s.local.yaml
	- kubectl apply -f gso.k8s.local.yaml
	- kubectl apply -f secrets.k8s.local.yaml
- Restart coreDns to apply the host header rewrite
	- kubectl -n kube-system rollout restart deployment coredns
