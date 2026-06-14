# Changelog

## [1.2.0](https://github.com/LekePerlaska/FlightKS-API/compare/v1.1.0...v1.2.0) (2026-06-14)


### Features

* add Caddy reverse proxy service for TLS termination ([97d1bf0](https://github.com/LekePerlaska/FlightKS-API/commit/97d1bf0d8252083ad7ab6b49a7d4bd632ec5d0ea))
* email delivery alongside SignalR notifications via MailKit ([c6a78dc](https://github.com/LekePerlaska/FlightKS-API/commit/c6a78dc107096cf895e8400401c1baac4eb1252e))
* export Keycloak realm and auto-import on startup ([6ae6fe9](https://github.com/LekePerlaska/FlightKS-API/commit/6ae6fe97b24a8c7bbc641bf2eb7815a5f59e119e))
* fill notification gaps across schedule, check-in, booking and ticket flows ([ca1864e](https://github.com/LekePerlaska/FlightKS-API/commit/ca1864e7b0202d85b0fac53dd831852e8ba8bb0b))
* FlightManager-to-Airline scoping ([8179ec6](https://github.com/LekePerlaska/FlightKS-API/commit/8179ec60f7a262a916df4cc0b86138ba346f6aed))
* FlightManager-to-Airline scoping ([817a659](https://github.com/LekePerlaska/FlightKS-API/commit/817a6596155895b063e296cfcbc2fcd4e023b5c4))
* implement itinerary system backend ([edb9586](https://github.com/LekePerlaska/FlightKS-API/commit/edb9586c29edfd2686fff136d17b23a1d087734f))
* implement real health checks for Postgres and Redis ([3301e07](https://github.com/LekePerlaska/FlightKS-API/commit/3301e073c6a1d8d12f72694be92980ccb20da26b))
* switch rate limiting to Distributed mode (Redis) ([947d527](https://github.com/LekePerlaska/FlightKS-API/commit/947d5273798a66c47b9606ad3ff109e071978c4a))
* switch rate limiting to Distributed mode (Redis) ([1a08d8b](https://github.com/LekePerlaska/FlightKS-API/commit/1a08d8bf3b7fd893f54494d48029133df39688ee))
* wire SignalR real-time notifications + composite index migration ([d1e4270](https://github.com/LekePerlaska/FlightKS-API/commit/d1e42709dcf3fe965f2c7fbfbc1879cbd0ae96f2))


### Bug Fixes

* 14 code-review findings — correctness, perf, auth, reliability ([8e9ca4f](https://github.com/LekePerlaska/FlightKS-API/commit/8e9ca4f2f2dd5bcca5b692eeaf1cbadf7de5f2d2))
* aircraft validation ([fe9240b](https://github.com/LekePerlaska/FlightKS-API/commit/fe9240bfa3d8c1216b1b0216c19c2bdf25e64a8c))
* aircraft validation ([f290420](https://github.com/LekePerlaska/FlightKS-API/commit/f2904204198e77d5e316c23be9a0f831f516278f))
* assign Keycloak User role on registration and fix baggage 500 ([2fd1a31](https://github.com/LekePerlaska/FlightKS-API/commit/2fd1a310d0aa977e1135143c2a6b1374dba4837c))
* block terminal ticket state reversal; separate PATCH from PUT on schedules ([c06f408](https://github.com/LekePerlaska/FlightKS-API/commit/c06f408eeb7c3e22a24252a49a012820d51a5233))
* block terminal ticket state reversal; separate PATCH from PUT on schedules ([c7c587c](https://github.com/LekePerlaska/FlightKS-API/commit/c7c587c6e117dc2e6ff7fadd7515f7ec8d6f7939))
* BookService nullable warnings ([91fb766](https://github.com/LekePerlaska/FlightKS-API/commit/91fb7663651f3c187784a3020dc67c8cfe874e20))
* data protection key pressitance ([43a5208](https://github.com/LekePerlaska/FlightKS-API/commit/43a5208777ec664972f1b69483def79338196714))
* **docker:** apt-get upgrade in runtime stage to clear OS CVEs ([5bba2c7](https://github.com/LekePerlaska/FlightKS-API/commit/5bba2c71ba28405b06cbb3b77f57ecf41e3099ed))
* **docker:** apt-get upgrade in runtime stage to clear OS CVEs ([968ef66](https://github.com/LekePerlaska/FlightKS-API/commit/968ef663650c02dec79f471be546291a1aa47fe4))
* keycloack validation ([49508ba](https://github.com/LekePerlaska/FlightKS-API/commit/49508baa4a7549218511cfec4bca7dd56eb7158c))
* persist uploaded files across container restarts ([87b7d08](https://github.com/LekePerlaska/FlightKS-API/commit/87b7d083e51ea8dbf88f8239d8c494d803d85eaa))
* resolve 10 code-review findings across services and infrastructure ([472ce7d](https://github.com/LekePerlaska/FlightKS-API/commit/472ce7d93d9f1b7133420628cc696b0516e248a9))
* user data access by id ([f769ded](https://github.com/LekePerlaska/FlightKS-API/commit/f769dedfa6f081096ce0ac5b05a296c2c19ead19))

## Changelog

All notable changes to this project will be documented in this file.
