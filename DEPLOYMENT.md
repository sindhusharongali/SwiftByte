# SwiftBite Deployment Guide

## Overview

This guide covers deploying the SwiftBite microservices system across different environments: development, staging, and production.

---

## Development Deployment

### Local Development (Windows/Linux/Mac)

**Prerequisites:**
- .NET 8.0 SDK
- Docker Desktop
- Git

**Steps:**

```bash
# 1. Clone repository
git clone <repository-url>
cd SwiftByte

# 2. Start infrastructure containers
docker-compose up -d

# 3. Verify containers running
docker ps

# 4. Restore and build
dotnet restore
dotnet build

# 5. Run services in separate terminals
# Terminal 1
cd OrderService && dotnet run

# Terminal 2
cd PaymentService && dotnet run

# Terminal 3
cd RestaurantService && dotnet run

# Terminal 4
cd SearchService && dotnet run
```

**Access Points:**
- OrderService: https://localhost:7001
- PaymentService: https://localhost:7002
- RestaurantService: https://localhost:7003
- SearchService: https://localhost:7004
- RabbitMQ Admin: http://localhost:15672
- Kibana: http://localhost:5601

**Stopping:**
```bash
# Stop services: Ctrl+C in each terminal
# Stop containers
docker-compose down
```

---

## Staging Deployment

### Docker Compose Deployment

**Setup:**

```bash
# 1. Build images
docker-compose -f docker-compose.yml build

# 2. Start services
docker-compose up -d

# 3. Verify health
docker-compose ps

# 4. View logs
docker-compose logs -f
```

**Update Services:**
```bash
# Rebuild and restart
docker-compose up -d --build
```

**Cleanup:**
```bash
docker-compose down -v  # Also remove volumes
```

---

## Production Deployment

### Kubernetes Deployment

#### Prerequisites
- Kubernetes cluster (1.24+)
- kubectl configured
- Docker registry access
- Helm 3+ (recommended)

#### Step 1: Build and Push Images

```bash
# Build images
docker build -t myregistry/orderservice:1.0.0 ./OrderService
docker build -t myregistry/paymentservice:1.0.0 ./PaymentService
docker build -t myregistry/restaurantservice:1.0.0 ./RestaurantService
docker build -t myregistry/searchservice:1.0.0 ./SearchService

# Push to registry
docker push myregistry/orderservice:1.0.0
docker push myregistry/paymentservice:1.0.0
docker push myregistry/restaurantservice:1.0.0
docker push myregistry/searchservice:1.0.0
```

#### Step 2: Kubernetes Manifests

Create `k8s-manifests/` directory with:

**k8s-manifests/namespace.yaml:**
```yaml
apiVersion: v1
kind: Namespace
metadata:
  name: swiftbite
```

**k8s-manifests/configmap.yaml:**
```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: swiftbite-config
  namespace: swiftbite
data:
  RABBITMQ_HOST: "rabbitmq-service.swiftbite.svc.cluster.local"
  RABBITMQ_PORT: "5672"
  REDIS_HOST: "redis-service.swiftbite.svc.cluster.local"
  REDIS_PORT: "6379"
  ELASTICSEARCH_URL: "http://elasticsearch-service.swiftbite.svc.cluster.local:9200"
```

**k8s-manifests/orderservice-deployment.yaml:**
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: orderservice
  namespace: swiftbite
spec:
  replicas: 3
  selector:
    matchLabels:
      app: orderservice
  template:
    metadata:
      labels:
        app: orderservice
    spec:
      containers:
      - name: orderservice
        image: myregistry/orderservice:1.0.0
        imagePullPolicy: Always
        ports:
        - containerPort: 80
          name: http
        - containerPort: 443
          name: https
        envFrom:
        - configMapRef:
            name: swiftbite-config
        resources:
          requests:
            memory: "256Mi"
            cpu: "100m"
          limits:
            memory: "512Mi"
            cpu: "500m"
        livenessProbe:
          httpGet:
            path: /health
            port: 80
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /health
            port: 80
          initialDelaySeconds: 5
          periodSeconds: 5
```

**k8s-manifests/orderservice-service.yaml:**
```yaml
apiVersion: v1
kind: Service
metadata:
  name: orderservice
  namespace: swiftbite
spec:
  selector:
    app: orderservice
  type: LoadBalancer
  ports:
  - name: http
    port: 80
    targetPort: 80
  - name: https
    port: 443
    targetPort: 443
```

Repeat similar configurations for PaymentService, RestaurantService, and SearchService.

#### Step 3: Deploy Infrastructure

**k8s-manifests/rabbitmq-statefulset.yaml:**
```yaml
apiVersion: apps/v1
kind: StatefulSet
metadata:
  name: rabbitmq
  namespace: swiftbite
spec:
  serviceName: rabbitmq
  replicas: 3
  selector:
    matchLabels:
      app: rabbitmq
  template:
    metadata:
      labels:
        app: rabbitmq
    spec:
      containers:
      - name: rabbitmq
        image: rabbitmq:3.12-management
        ports:
        - containerPort: 5672
          name: amqp
        - containerPort: 15672
          name: management
        env:
        - name: RABBITMQ_DEFAULT_USER
          valueFrom:
            secretKeyRef:
              name: rabbitmq-secret
              key: username
        - name: RABBITMQ_DEFAULT_PASS
          valueFrom:
            secretKeyRef:
              name: rabbitmq-secret
              key: password
        resources:
          requests:
            memory: "512Mi"
            cpu: "500m"
          limits:
            memory: "1Gi"
            cpu: "1000m"
```

#### Step 4: Deploy to Cluster

```bash
# Create namespace
kubectl apply -f k8s-manifests/namespace.yaml

# Create secrets
kubectl create secret generic rabbitmq-secret \
  --from-literal=username=admin \
  --from-literal=password=secure-password \
  -n swiftbite

# Deploy infrastructure
kubectl apply -f k8s-manifests/

# Monitor deployment
kubectl get pods -n swiftbite -w

# Check service status
kubectl get svc -n swiftbite
```

---

## Cloud Deployment

### Azure Container Instances (ACI)

```bash
# Create resource group
az group create --name swiftbite --location eastus

# Create container instances
az container create \
  --resource-group swiftbite \
  --name orderservice \
  --image myregistry/orderservice:1.0.0 \
  --registry-login-server myregistry.azurecr.io \
  --registry-username admin \
  --registry-password <password> \
  --environment-variables \
    RABBITMQ_HOST=rabbitmq.example.com \
    REDIS_HOST=redis.example.com \
  --ports 80 443
```

### AWS ECS (Elastic Container Service)

```bash
# Create task definition
aws ecs register-task-definition \
  --family orderservice \
  --container-definitions file://task-definition.json

# Create service
aws ecs create-service \
  --cluster swiftbite \
  --service-name orderservice \
  --task-definition orderservice \
  --desired-count 3
```

### Google Cloud Run

```bash
# Build and push
gcloud builds submit --tag gcr.io/PROJECT_ID/orderservice:1.0.0

# Deploy
gcloud run deploy orderservice \
  --image gcr.io/PROJECT_ID/orderservice:1.0.0 \
  --platform managed \
  --region us-central1 \
  --set-env-vars RABBITMQ_HOST=rabbitmq.example.com
```

---

## Monitoring & Logging

### Kubernetes Monitoring

**Install Prometheus:**
```bash
helm repo add prometheus-community https://prometheus-community.github.io/helm-charts
helm install prometheus prometheus-community/kube-prometheus-stack -n swiftbite
```

**Install Grafana:**
```bash
helm repo add grafana https://grafana.github.io/helm-charts
helm install grafana grafana/grafana -n swiftbite
```

### Logging

**Deploy ELK Stack:**
```bash
# Elasticsearch
helm install elasticsearch elastic/elasticsearch -n swiftbite

# Kibana
helm install kibana elastic/kibana -n swiftbite

# Filebeat (log collection)
helm install filebeat elastic/filebeat -n swiftbite
```

---

## Health Checks

### Add Health Check Endpoints

```csharp
// In Program.cs
builder.Services.AddHealthChecks()
    .AddRabbitMQ()
    .AddRedis()
    .AddElasticsearch();

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = (check) => check.Tags.Contains("ready")
});
```

### Kubernetes Health Checks

Update deployments with:
```yaml
livenessProbe:
  httpGet:
    path: /health
    port: 80
  initialDelaySeconds: 30
  periodSeconds: 10
  
readinessProbe:
  httpGet:
    path: /health/ready
    port: 80
  initialDelaySeconds: 5
  periodSeconds: 5
```

---

## Auto-Scaling

### Kubernetes Horizontal Pod Autoscaler

```yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: orderservice-hpa
  namespace: swiftbite
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: orderservice
  minReplicas: 2
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
  - type: Resource
    resource:
      name: memory
      target:
        type: Utilization
        averageUtilization: 80
```

---

## Database Migration (Future)

When upgrading from in-memory to SQL Server:

```bash
# Create migration
dotnet ef migrations add AddSagaState --project OrderService

# Apply migration
dotnet ef database update --project OrderService
```

---

## Backup & Disaster Recovery

### Backup Strategy

```bash
# Backup Elasticsearch indices
curl -X PUT "localhost:9200/_snapshot/backup" \
  -H 'Content-Type: application/json' \
  -d'{"type": "fs", "settings": {"location": "/mnt/backup"}}'

# Backup Redis
redis-cli BGSAVE

# Backup RabbitMQ definitions
rabbitmqctl export_definitions /backup/rabbitmq-definitions.json
```

### Restore Procedure

```bash
# Restore Elasticsearch
curl -X POST "localhost:9200/_snapshot/backup/snapshot_1/_restore"

# Restore Redis
cp /backup/dump.rdb /var/lib/redis/
redis-server --dir /var/lib/redis

# Restore RabbitMQ
rabbitmqctl import_definitions /backup/rabbitmq-definitions.json
```

---

## Performance Tuning

### RabbitMQ Optimization

```bash
# Increase file descriptors
rabbitmqctl eval 'file_handle_cache:get_limit().'

# Configure prefetch
rabbitmq.conf:
channel_max = 2048
connection_max = unlimited
memory_high_watermark.relative = 0.6
```

### Elasticsearch Optimization

```yaml
# elasticsearch.yml
index.number_of_shards: 5
index.number_of_replicas: 1
indices.queries.cache.size: 30%
thread_pool.search.queue_size: 1000
```

### Redis Optimization

```bash
# redis.conf
maxmemory: 2gb
maxmemory-policy: allkeys-lru
tcp-backlog: 511
timeout: 300
```

---

## Rollback Procedure

### Kubernetes Rollback

```bash
# View rollout history
kubectl rollout history deployment/orderservice -n swiftbite

# Rollback to previous version
kubectl rollout undo deployment/orderservice -n swiftbite

# Rollback to specific revision
kubectl rollout undo deployment/orderservice \
  --to-revision=3 -n swiftbite
```

### Docker Compose Rollback

```bash
# Revert to previous image version in docker-compose.yml
# Then redeploy
docker-compose down
docker-compose up -d
```

---

## Troubleshooting Deployment

### Pod not starting
```bash
kubectl describe pod orderservice-xxxx -n swiftbite
kubectl logs orderservice-xxxx -n swiftbite
```

### Service not responding
```bash
# Check service
kubectl get svc -n swiftbite

# Check endpoints
kubectl get endpoints -n swiftbite

# Port forward for testing
kubectl port-forward svc/orderservice 8000:80 -n swiftbite
```

### High resource usage
```bash
# Check resource usage
kubectl top pods -n swiftbite

# Scale deployment
kubectl scale deployment orderservice --replicas=5 -n swiftbite
```

---

## Maintenance

### Regular Tasks

- **Daily:** Monitor logs and alerts
- **Weekly:** Review performance metrics
- **Monthly:** Test backup/restore procedures
- **Quarterly:** Security updates and patches
- **Yearly:** Capacity planning

### Update Strategy

```bash
# 1. Update in staging
docker-compose -f docker-compose.staging.yml up -d --build

# 2. Run smoke tests
./run-smoke-tests.sh

# 3. Deploy to production with blue-green deployment
kubectl apply -f k8s-manifests/orderservice-v2.yaml

# 4. Switch traffic
kubectl patch service orderservice -p '{"spec":{"selector":{"version":"v2"}}}'

# 5. Monitor
kubectl logs -f deployment/orderservice -n swiftbite

# 6. Keep v1 for quick rollback
# 7. Cleanup old version after stability
```

---

## Support

For deployment issues:
1. Check [README.md](README.md) for common problems
2. Review logs using `kubectl logs` or `docker logs`
3. Consult [ARCHITECTURE.md](ARCHITECTURE.md) for design details
4. Contact DevOps team

---

**Last Updated:** January 3, 2026
