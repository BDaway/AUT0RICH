@echo off
setlocal EnableDelayedExpansion

:: ���ýű�����Ŀ¼
set "WORK_DIR=%~dp0"
set "VENV_DIR=%WORK_DIR%python_env\venv"
set "PY_FILE=%WORK_DIR%paddleocr_server.py"

:: ������Ļ
cls
echo ==================================================
echo      һ������ OCR ����
echo ==================================================
echo.

:: ��� Python �ļ��Ƿ����
if not exist "%PY_FILE%" (
    echo [����] δ�ҵ� Python �ļ���%PY_FILE%
    echo ��ȷ�� ocr_service.py �����ڽű�Ŀ¼��
    pause
    exit /b 1
)

:: ������⻷���Ƿ����
if not exist "%VENV_DIR%\Scripts\activate.bat" (
    echo [����] δ�ҵ����⻷����%VENV_DIR%
    echo �������� setup_python_env.bat ����������
    pause
    exit /b 1
)

:: �������⻷��
echo ���ڼ������⻷�� ...
call "%VENV_DIR%\Scripts\activate.bat"
if %ERRORLEVEL% neq 0 (
    echo [����] �������⻷��ʧ�ܣ�
    pause
    exit /b 1
)

:: ��ʾ Python �汾
echo.
echo ��ǰ���� Python �汾��
python --version
echo.

:: ����Ҫ���Ƿ�װ
echo ���ڼ���Ҫ�� ...
set "MISSING_PACKAGES="
for %%p in (flask opencv-python paddlepaddle-gpu paddleocr) do (
    pip show %%p >nul 2>&1
    if !ERRORLEVEL! neq 0 (
        set "MISSING_PACKAGES=!MISSING_PACKAGES! %%p"
    )
)
if defined MISSING_PACKAGES (
    echo [����] ȱ�����°���%MISSING_PACKAGES%
    echo ������ install_packages.bat ��װ��Ҫ����
    pause
    deactivate
    exit /b 1
)

:: ��� GPU �����ԣ�����ʾ����ǿ�ƣ�
echo ��� GPU �����ԣ������ο�����
python -c "import paddle; print('Compiled with CUDA:', paddle.device.is_compiled_with_cuda()); print('Current Device:', paddle.device.get_device())" 2>nul
if %ERRORLEVEL% neq 0 (
    echo [����] �޷���� GPU �����ԣ�����ȱ�� paddlepaddle-gpu ���������⡣
    echo ��������Կ����У�ʹ�� CPU �� GPU����
)
echo.

:: ���� Python ����
echo �������� OCR ���� ...
echo ���������� http://localhost:8000/recognize
echo.

:: ��ʾ���Է���
echo ���Է�����
echo 1. ȷ���������������� Flask ��־����
echo 2. ʹ�� curl �� Postman ���� POST ����
echo    curl -F "image=@test.jpg" -F "numbersOnly=true" http://localhost:8000/recognize
echo 3. �� Ctrl+C ֹͣ����
echo.

:: ִ�� Python �ļ�
python "%PY_FILE%"
if %ERRORLEVEL% neq 0 (
    echo [����] ��������ʧ�ܣ�
    echo - �����ʾ�˿�ռ�ã�Address already in use�������� 8000 �˿ڡ�
    echo - �����ʾģ��ȱʧ�������� install_packages.bat��
    echo - �����ʾ GPU ������ȷ�� CUDA 11.8 �� cuDNN ���á�
    pause
)

:: �˳����⻷��
deactivate
echo.
echo ������ֹͣ��
echo ��������˳�...
pause >nul
exit /b 0