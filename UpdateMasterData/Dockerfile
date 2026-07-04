#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
#RUN apt-get update && apt-get install -y libskia-sharp
#修改為8888 Port , JOB 使用 8888 Port
EXPOSE 8888

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# 複製 csproj 並還原相依性
# COPY ["UpdateMasterData.csproj", "."]
# 複製所有源代碼和 DLL 元件



# 複製所有源代碼和 DLL 元件
COPY . .

RUN dotnet restore "project/job/SetHQAttendJob/UpdateMasterData/UpdateMasterData.csproj"

#安裝時區資訊
RUN apt-get update \
    &&  DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends tzdata

# 設定時區
RUN TZ=Asia/Taipei \
    && ln -snf /usr/share/zoneinfo/$TZ /etc/localtime \
    && echo $TZ > /etc/timezone \
    && dpkg-reconfigure -f noninteractive tzdata 

# 建置應用程式
RUN dotnet build "project/job/SetHQAttendJob/UpdateMasterData/UpdateMasterData.csproj" -c $BUILD_CONFIGURATION -o /app/build


FROM build AS publish
ARG BUILD_CONFIGURATION=Release
# 發行應用程式
RUN dotnet publish "project/job/SetHQAttendJob/UpdateMasterData//UpdateMasterData.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
# 複製發佈結果
COPY --from=publish /app/publish .

# 複製jf open 粉圓字型到字型目錄(JOB不需要)
# COPY ./Fonts/jf-openhuninn-2.0.ttf /usr/share/fonts/
# 更新字型緩存
#RUN fc-cache -fv

# 設定進入點
ENTRYPOINT ["dotnet", "UpdateMasterData.dll"]