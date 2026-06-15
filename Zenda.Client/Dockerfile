# Etapa 1: Compilación
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish "Zenda.Client/Zenda.Client.csproj" -c Release -o /app/publish

# Etapa 2: Servidor de archivos estáticos (Nginx)
FROM nginx:alpine
WORKDIR /usr/share/nginx/html
# Copiamos los archivos de Blazor al servidor web
COPY --from=build /app/publish/wwwroot .
# Copiamos una configuración básica para que Blazor maneje las rutas
COPY Zenda.Client/nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]