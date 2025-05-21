# READ ME
Author: WillyPain
Date: 25/04/2025

## Dev Build Steps
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
- Install nginx ingress controller (With Helm)
	- helm repo add ingress-nginx https://kubernetes.github.io/ingress-nginx 
	- helm repo update
	- `helm install ingress-nginx ingress-nginx/ingress-nginx --namespace ingress-nginx --create-namespace --set controller.extraArgs.default-ssl-certificate=ingress-nginx/susine.dev`
- Create secret for dev cert
	- kubectl create secret tls susine.dev --key susine.dev.key --cert susine.dev.crt -n ingress-nginx
- Install Linkerd Service Mesh (With Helm)
	- helm repo add linkerd-edge https://helm.linkerd.io/edge
	- `helm install linkerd-crds linkerd-edge/linkerd-crds -n linkerd --create-namespace`
	- step certificate create root.linkerd.cluster.local ca.crt ca.key --profile root-ca --no-password --insecure
	- step certificate create identity.linkerd.cluster.local issuer.crt issuer.key --profile intermediate-ca --not-after 8760h --no-password --insecure --ca ca.crt --ca-key ca.key
	- `helm install linkerd-control-plane -n linkerd --set-file identityTrustAnchorsPEM=ca.crt --set-file identity.issuer.tls.crtPEM=issuer.crt --set-file identity.issuer.tls.keyPEM=issuer.key linkerd-edge/linkerd-control-plane`
- Apply the following k8s manifest files in Docker folder
	- kubectl rbac.local.k8s.ignore.yaml (not playing nice with skaffold atm)
- Run Skaffold build or Skaffold dev (if you want to watch changes)
- Restart coreDns to apply the host header rewrite
	- kubectl -n kube-system rollout restart deployment coredns


## k3s server stuff
- Little guide for setting up a baremetal k3s cluster on a linex server
- TODO: may as well make this a script at some point (ill wait to iron out the kinks first)

#### Installing k3s cluster
- Install k3s -> `sudo curl -sfL https://get.k3s.io | INSTALL_K3S_EXEC="--tls-san <server ip> --node-ip <server ip> --disable traefik" sh -s -`

#### Kube'n from home
- Yoink the cluster config from k3s server `sudo cat /etc/rancher/k3s/k3s.yaml`
- Copy it to on dev machine `~\.kube\k3s.yaml`
- Add new config file to env variable (dev machine) `KUBECONFIG = ~\.kube\config;~\.kube\k3s.yaml`

#### Setup local dev image registry 
- k3s docs -> https://docs.k3s.io/installation/private-registry
- Edit `/etc/rancher/k3s/registries.yaml`
	`mirrors:
		"<registry url>": #NOTE: dont add scheme here!
			endpoint:
				- "<scheme>://<registry url>"`
- I host a local registry using registry v2
	- `docker run -d -p 5000:5000 --restart=always --name registry registry:2`
	- The registry url would be "<local ip addres>:5000"
	- Then push your images to the registry
