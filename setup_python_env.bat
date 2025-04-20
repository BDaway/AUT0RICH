@echo off
setlocal EnableDelayedExpansion

:: 设置脚本工作目录
set "WORK_DIR=%~dp0"
set "ENV_DIR=%WORK_DIR%python_env"
set "DOWNLOAD_DIR=%WORK_DIR%downloads"
set "PYTHON_MIRROR_URL=https://mirrors.aliyun.com/python-release/windows"

:: 清理屏幕
cls
echo ==================================================
echo      一键配置独立 Python 环境（阿里云镜像版）
echo ==================================================
echo.

:: 检查是否已安装 curl
where curl >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo [错误] 未找到 curl，请先安装 curl！
    echo 你可以从 https://curl.se/windows/ 下载并配置环境变量。
    pause
    exit /b 1
)

:: 提供 Python 版本选择
echo 请选择要安装的 Python 版本：
echo 1. Python 3.12
echo 2. Python 3.11
echo 3. Python 3.10
echo 4. Python 3.9
echo 5. Python 3.8
echo 6. Python 3.7
set /p choice="请输入编号（1-6）："

:: 映射选择到版本号和安装包名
if "%choice%"=="1" set "PY_VERSION=3.12.7" & set "PY_INSTALLER=python-3.12.7-amd64.exe"
if "%choice%"=="2" set "PY_VERSION=3.11.10" & set "PY_INSTALLER=python-3.11.10-amd64.exe"
if "%choice%"=="3" set "PY_VERSION=3.10.11" & set "PY_INSTALLER=python-3.10.11-amd64.exe"
if "%choice%"=="4" set "PY_VERSION=3.9.13" & set "PY_INSTALLER=python-3.9.13-amd64.exe"
if "%choice%"=="5" set "PY_VERSION=3.8.10" & set "PY_INSTALLER=python-3.8.10-amd64.exe"
if "%choice%"=="6" set "PY_VERSION=3.7.9" & set "PY_INSTALLER=python-3.7.9-amd64.exe"
if not defined PY_VERSION (
    echo [错误] 无效的选择！
    pause
    exit /b 1
)

:: 设置下载链接
set "PY_URL=%PYTHON_MIRROR_URL%/%PY_INSTALLER%"

:: 创建必要的目录
if not exist "%DOWNLOAD_DIR%" mkdir "%DOWNLOAD_DIR%"
if not exist "%ENV_DIR%" mkdir "%ENV_DIR%"

:: 下载 Python 完整安装包
echo.
echo 正在从阿里云镜像下载 Python %PY_VERSION% 安装包 ...
curl -o "%DOWNLOAD_DIR%\%PY_INSTALLER%" "%PY_URL%"
if %ERRORLEVEL% neq 0 (
    echo [错误] 下载 Python 安装包失败！请检查网络或确认版本是否在阿里云镜像可用。
    pause
    exit /b 1
)

:: 静默安装 Python 到本地目录（不影响系统）
echo 正在安装 Python 到本地环境 ...
start /wait "" "%DOWNLOAD_DIR%\%PY_INSTALLER%" /quiet InstallAllUsers=0 TargetDir="%ENV_DIR%\python" Include_pip=0 Include_test=0
if %ERRORLEVEL% neq 0 (
    echo [错误] 安装 Python 失败！
    pause
    exit /b 1
)

:: 使用 ensurepip 安装 pip
echo 正在使用 ensurepip 安装 pip ...
"%ENV_DIR%\python\python.exe" -m ensurepip --default-pip
if %ERRORLEVEL% neq 0 (
    echo [错误] 使用 ensurepip 安装 pip 失败！请检查 Python 安装包完整性或尝试其他版本。
    pause
    exit /b 1
)

:: 创建虚拟环境
echo 正在创建虚拟环境 ...
"%ENV_DIR%\python\python.exe" -m venv "%ENV_DIR%\venv"
if %ERRORLEVEL% neq 0 (
    echo [错误] 创建虚拟环境失败！
    pause
    exit /b 1
)

:: 升级 pip（使用阿里云镜像）
echo 正在升级 pip ...
"%ENV_DIR%\venv\Scripts\python.exe" -m pip install --upgrade pip -i https://mirrors.aliyun.com/pypi/simple/
if %ERRORLEVEL% neq 0 (
    echo [警告] 升级 pip 失败，但环境可能仍可用。
)

:: 创建激活脚本
echo @echo off> "%ENV_DIR%\activate_env.bat"
echo setlocal>> "%ENV_DIR%\activate_env.bat"
echo set "PATH=%ENV_DIR%\venv\Scripts;%%PATH%%">> "%ENV_DIR%\activate_env.bat"
echo cmd /k>> "%ENV_DIR%\activate_env.bat"

:: 清理下载文件
echo 正在清理临时文件 ...
rd /s /q "%DOWNLOAD_DIR%"

:: 完成提示
echo.
echo ==================================================
echo      配置完成！
echo ==================================================
echo.
echo 已成功创建独立的 Python %PY_VERSION% 环境！
echo 环境位置：%ENV_DIR%\venv
echo.
echo 使用方法：
echo 1. 双击 "%ENV_DIR%\activate_env.bat" 激活环境。
echo 2. 在命令行中即可使用 Python 和 pip。
echo 3. 输入 "exit" 退出环境。
echo.
echo 注意：pip 已配置为使用阿里云镜像，安装包速度更快！
echo.
echo 按任意键退出...
pause >nul
exit /b 0