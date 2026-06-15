# Etapa 1: Compilación (Usamos el SDK oficial)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
# Publicamos directamente
RUN dotnet publish "Zenda.Client/Zenda.Client.csproj" -c Release -o /app/publish

# Etapa 2: Servidor de archivos estáticos (Nginx)
FROM nginx:alpine
WORKDIR /usr/share/nginx/html
# Copiamos los archivos estáticos generados por Blazor
COPY --from=build /app/publish/wwwroot .
# Copiamos la configuración de Nginx para el enrutamiento de la SPA
COPY Zenda.Client/nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]