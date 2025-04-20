@echo off
setlocal EnableDelayedExpansion

:: ���ýű�����Ŀ¼
set "WORK_DIR=%~dp0"
set "ENV_DIR=%WORK_DIR%python_env"
set "DOWNLOAD_DIR=%WORK_DIR%downloads"
set "PYTHON_MIRROR_URL=https://mirrors.aliyun.com/python-release/windows"

:: ������Ļ
cls
echo ==================================================
echo      һ�����ö��� Python �����������ƾ���棩
echo ==================================================
echo.

:: ����Ƿ��Ѱ�װ curl
where curl >nul 2>&1
if %ERRORLEVEL% neq 0 (
    echo [����] δ�ҵ� curl�����Ȱ�װ curl��
    echo ����Դ� https://curl.se/windows/ ���ز����û���������
    pause
    exit /b 1
)

:: �ṩ Python �汾ѡ��
echo ��ѡ��Ҫ��װ�� Python �汾��
echo 1. Python 3.12
echo 2. Python 3.11
echo 3. Python 3.10
echo 4. Python 3.9
echo 5. Python 3.8
echo 6. Python 3.7
set /p choice="�������ţ�1-6����"

:: ӳ��ѡ�񵽰汾�źͰ�װ����
if "%choice%"=="1" set "PY_VERSION=3.12.7" & set "PY_INSTALLER=python-3.12.7-amd64.exe"
if "%choice%"=="2" set "PY_VERSION=3.11.10" & set "PY_INSTALLER=python-3.11.10-amd64.exe"
if "%choice%"=="3" set "PY_VERSION=3.10.11" & set "PY_INSTALLER=python-3.10.11-amd64.exe"
if "%choice%"=="4" set "PY_VERSION=3.9.13" & set "PY_INSTALLER=python-3.9.13-amd64.exe"
if "%choice%"=="5" set "PY_VERSION=3.8.10" & set "PY_INSTALLER=python-3.8.10-amd64.exe"
if "%choice%"=="6" set "PY_VERSION=3.7.9" & set "PY_INSTALLER=python-3.7.9-amd64.exe"
if not defined PY_VERSION (
    echo [����] ��Ч��ѡ��
    pause
    exit /b 1
)

:: ������������
set "PY_URL=%PYTHON_MIRROR_URL%/%PY_INSTALLER%"

:: ������Ҫ��Ŀ¼
if not exist "%DOWNLOAD_DIR%" mkdir "%DOWNLOAD_DIR%"
if not exist "%ENV_DIR%" mkdir "%ENV_DIR%"

:: ���� Python ������װ��
echo.
echo ���ڴӰ����ƾ������� Python %PY_VERSION% ��װ�� ...
curl -o "%DOWNLOAD_DIR%\%PY_INSTALLER%" "%PY_URL%"
if %ERRORLEVEL% neq 0 (
    echo [����] ���� Python ��װ��ʧ�ܣ����������ȷ�ϰ汾�Ƿ��ڰ����ƾ�����á�
    pause
    exit /b 1
)

:: ��Ĭ��װ Python ������Ŀ¼����Ӱ��ϵͳ��
echo ���ڰ�װ Python �����ػ��� ...
start /wait "" "%DOWNLOAD_DIR%\%PY_INSTALLER%" /quiet InstallAllUsers=0 TargetDir="%ENV_DIR%\python" Include_pip=0 Include_test=0
if %ERRORLEVEL% neq 0 (
    echo [����] ��װ Python ʧ�ܣ�
    pause
    exit /b 1
)

:: ʹ�� ensurepip ��װ pip
echo ����ʹ�� ensurepip ��װ pip ...
"%ENV_DIR%\python\python.exe" -m ensurepip --default-pip
if %ERRORLEVEL% neq 0 (
    echo [����] ʹ�� ensurepip ��װ pip ʧ�ܣ����� Python ��װ�������Ի��������汾��
    pause
    exit /b 1
)

:: �������⻷��
echo ���ڴ������⻷�� ...
"%ENV_DIR%\python\python.exe" -m venv "%ENV_DIR%\venv"
if %ERRORLEVEL% neq 0 (
    echo [����] �������⻷��ʧ�ܣ�
    pause
    exit /b 1
)

:: ���� pip��ʹ�ð����ƾ���
echo �������� pip ...
"%ENV_DIR%\venv\Scripts\python.exe" -m pip install --upgrade pip -i https://mirrors.aliyun.com/pypi/simple/
if %ERRORLEVEL% neq 0 (
    echo [����] ���� pip ʧ�ܣ������������Կ��á�
)

:: ��������ű�
echo @echo off> "%ENV_DIR%\activate_env.bat"
echo setlocal>> "%ENV_DIR%\activate_env.bat"
echo set "PATH=%ENV_DIR%\venv\Scripts;%%PATH%%">> "%ENV_DIR%\activate_env.bat"
echo cmd /k>> "%ENV_DIR%\activate_env.bat"

:: ���������ļ�
echo ����������ʱ�ļ� ...
rd /s /q "%DOWNLOAD_DIR%"

:: �����ʾ
echo.
echo ==================================================
echo      ������ɣ�
echo ==================================================
echo.
echo �ѳɹ����������� Python %PY_VERSION% ������
echo ����λ�ã�%ENV_DIR%\venv
echo.
echo ʹ�÷�����
echo 1. ˫�� "%ENV_DIR%\activate_env.bat" �������
echo 2. ���������м���ʹ�� Python �� pip��
echo 3. ���� "exit" �˳�������
echo.
echo ע�⣺pip ������Ϊʹ�ð����ƾ��񣬰�װ���ٶȸ��죡
echo.
echo ��������˳�...
pause >nul
exit /b 0