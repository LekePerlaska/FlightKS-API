# Uptime Kuma Monitors

Uptime Kuma runs in the Docker network, so monitor internal service names where possible. Create these monitors manually in the Uptime Kuma UI at `http://localhost:3002`.

## Recommended Monitors

| Name | Type | URL / Target | Expected |
| --- | --- | --- | --- |
| FlightKS frontend through Nginx | HTTP(s) | `http://nginx/` | 200 |
| FlightKS API health | HTTP(s) | `http://api:5194/health` | 200 |
| Keycloak realm | HTTP(s) | `http://keycloak:8080/realms/flightks` | 200 |
| Grafana health | HTTP(s) | `http://grafana:3000/api/health` | 200 |
| Prometheus ready | HTTP(s) | `http://prometheus:9090/-/ready` | 200 |
| Nginx reverse proxy port | TCP Port | Host `nginx`, port `80` | Open |

Keep Uptime Kuma configuration in the UI for now. This stays simple for Docker Compose and maps cleanly to future Kubernetes probes or an external uptime stack later.
