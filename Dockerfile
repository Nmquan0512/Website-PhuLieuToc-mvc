# ---------- Stage 1: Build ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy file dự án và restore các package
# Nếu file dự án của bạn có tên khác, hãy chỉnh sửa lại tên ở đây.
COPY ["PhuLieuToc.csproj", "./"]
RUN dotnet restore "PhuLieuToc.csproj"

# Copy toàn bộ mã nguồn và build ứng dụng
COPY . .
RUN dotnet publish "PhuLieuToc.csproj" -c Release -o /app/publish

# ---------- Stage 2: Runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copy ứng dụng đã publish từ stage build
COPY --from=build /app/publish .

# Expose cổng ứng dụng (80 cho HTTP, 443 nếu cần HTTPS)
EXPOSE 80

# Thiết lập biến môi trường để ASP.NET Core lắng nghe trên cổng 80
ENV ASPNETCORE_URLS=http://+:80

# Entry point để chạy ứng dụng
ENTRYPOINT ["dotnet", "PhuLieuToc.dll"]
