FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

# expose the port on which you are running web server in your application
EXPOSE 8080


WORKDIR /source


# Copy fsproj and restore all dependencies
COPY ./*.fsproj ./
RUN dotnet restore


# Copy source code and build / publish app and libraries
COPY . .
RUN dotnet publish -c release -o /app


# **Run project**
# Create new layer with runtime, copy app / libraries, then run dotnet
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "ArbitrageGainer-T8.dll"]