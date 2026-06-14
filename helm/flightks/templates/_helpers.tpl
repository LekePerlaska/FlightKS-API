{{/*
Shared template helpers for the FlightKS chart.
Service DNS names produced here are what every component uses to reach its
dependencies inside the cluster (e.g. the backend connects to Postgres at
"<fullname>-postgres"), so they must stay consistent across templates.
*/}}

{{/* Base name (chart name, overridable). */}}
{{- define "flightks.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{/*
Fully qualified app name. Used as the prefix for every resource and service
name. If the release name already contains the chart name we don't repeat it,
so `helm install flightks ./helm/flightks` yields clean names like
"flightks-backend" rather than "flightks-flightks-backend".
*/}}
{{- define "flightks.fullname" -}}
{{- if .Values.fullnameOverride -}}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" -}}
{{- else -}}
{{- $name := default .Chart.Name .Values.nameOverride -}}
{{- if contains $name .Release.Name -}}
{{- .Release.Name | trunc 63 | trimSuffix "-" -}}
{{- else -}}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" -}}
{{- end -}}
{{- end -}}
{{- end -}}

{{/* Chart label value, e.g. "flightks-0.1.0". */}}
{{- define "flightks.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{/* Common labels applied to every resource. */}}
{{- define "flightks.labels" -}}
helm.sh/chart: {{ include "flightks.chart" . }}
{{ include "flightks.selectorLabels" . }}
{{- if .Chart.AppVersion }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
{{- end }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
app.kubernetes.io/part-of: flightks
{{- end -}}

{{/* Selector labels — stable subset, never include version. */}}
{{- define "flightks.selectorLabels" -}}
app.kubernetes.io/name: {{ include "flightks.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end -}}

{{/* Namespace all resources are pinned to — the release namespace (set via `helm -n <ns>`). */}}
{{- define "flightks.namespace" -}}
{{- .Release.Namespace -}}
{{- end -}}

{{/* Shared object names. */}}
{{- define "flightks.configName" -}}{{ include "flightks.fullname" . }}-config{{- end -}}
{{- define "flightks.secretName" -}}{{ include "flightks.fullname" . }}-secrets{{- end -}}

{{/* ServiceAccount name (override via serviceAccount.name, else the fullname). */}}
{{- define "flightks.serviceAccountName" -}}
{{- default (include "flightks.fullname" .) .Values.serviceAccount.name -}}
{{- end -}}

{{/* Per-component resource / service names. */}}
{{- define "flightks.backend.fullname" -}}{{ include "flightks.fullname" . }}-backend{{- end -}}
{{- define "flightks.frontend.fullname" -}}{{ include "flightks.fullname" . }}-frontend{{- end -}}
{{- define "flightks.postgres.fullname" -}}{{ include "flightks.fullname" . }}-postgres{{- end -}}
{{- define "flightks.redis.fullname" -}}{{ include "flightks.fullname" . }}-redis{{- end -}}
{{- define "flightks.keycloak.fullname" -}}{{ include "flightks.fullname" . }}-keycloak{{- end -}}

{{/* Public origin the browser uses (ingress host) — basis for the Keycloak issuer and CORS. */}}
{{- define "flightks.publicOrigin" -}}
{{- printf "%s://%s" .Values.publicScheme .Values.ingress.host -}}
{{- end -}}

{{/* Internal Keycloak realm base URL (in-cluster, service DNS). */}}
{{- define "flightks.keycloak.internalRealmUrl" -}}
{{- printf "http://%s:%v/realms/%s" (include "flightks.keycloak.fullname" .) .Values.keycloak.port .Values.keycloak.realm -}}
{{- end -}}

{{/* Image pull secrets block (reused by every workload). */}}
{{- define "flightks.imagePullSecrets" -}}
{{- with .Values.image.pullSecrets }}
imagePullSecrets:
{{- range . }}
  - name: {{ . }}
{{- end }}
{{- end }}
{{- end -}}
