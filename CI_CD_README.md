# CI/CD Pipeline cho MovieTheater Web App (Không sử dụng Kubernetes)

## Tổng quan
Pipeline này tự động build, test và deploy ứng dụng ASP.NET Core lên các server thông qua Docker và SSH.

## Cấu trúc Pipeline

### 1. Build Stage
- Build ứng dụng với .NET 8.0
- Publish artifacts
- Build Docker image và push lên GitLab Container Registry

### 2. Test Stage
- Chạy unit tests
- Kiểm tra code quality

### 3. Deploy Stage
- **Staging**: Tự động deploy khi push vào branch `develop`
- **Production**: Manual deploy khi push vào branch `main`

## Cách sử dụng

### 1. Setup GitLab Variables
Trong GitLab project, vào Settings > CI/CD > Variables và thêm:

**Registry Variables:**
- `CI_REGISTRY_USER`: GitLab registry username
- `CI_REGISTRY_PASSWORD`: GitLab registry password
- `CI_REGISTRY`: GitLab registry URL
- `CI_REGISTRY_IMAGE`: Registry image path

**Staging Server Variables:**
- `STAGING_HOST`: IP hoặc domain của staging server
- `STAGING_USER`: SSH username cho staging server
- `STAGING_SSH_PRIVATE_KEY`: Private key để SSH vào staging server
- `STAGING_SSH_KNOWN_HOSTS`: SSH known hosts cho staging server

**Production Server Variables:**
- `PRODUCTION_HOST`: IP hoặc domain của production server
- `PRODUCTION_USER`: SSH username cho production server
- `PRODUCTION_SSH_PRIVATE_KEY`: Private key để SSH vào production server
- `PRODUCTION_SSH_KNOWN_HOSTS`: SSH known hosts cho production server

### 2. Server Setup
Trên mỗi server (staging/production), cần:
```bash
# Cài đặt Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh

# Thêm user vào docker group
sudo usermod -aG docker $USER

# Tạo SSH key pair cho GitLab CI
ssh-keygen -t rsa -b 4096 -C "gitlab-ci@yourdomain.com"
```

### 3. Branch Strategy
- `develop`: Tự động deploy lên staging
- `main`: Manual deploy lên production

### 4. Local Development
```bash
# Chạy với Docker Compose
docker-compose up -d

# Build Docker image
docker build -t movietheater .

# Chạy container
docker run -p 5000:80 movietheater
```

## Monitoring
- GitLab CI/CD: Xem pipeline status trong GitLab
- Application logs: `docker logs movietheater-staging` hoặc `docker logs movietheater-production`
- Health checks: `/health` endpoint

## Troubleshooting
1. **Build fails**: Kiểm tra .NET version và dependencies
2. **Deploy fails**: 
   - Kiểm tra SSH connection và credentials
   - Kiểm tra Docker registry credentials
   - Kiểm tra server có đủ disk space không
3. **App not starting**: Kiểm tra connection string và environment variables

## Security Notes
- Không commit secrets vào repository
- Sử dụng GitLab CI/CD variables cho sensitive data
- Enable branch protection rules cho main branch
- Sử dụng SSH keys thay vì password
- Giới hạn quyền truy cập SSH cho GitLab CI user 