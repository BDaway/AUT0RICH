@echo off
setlocal EnableDelayedExpansion

:: ���ýű�����Ŀ¼
set "WORK_DIR=%~dp0"
set "VENV_DIR=%WORK_DIR%python_env\venv"
set "TSINGHUA_MIRROR=https://pypi.tuna.tsinghua.edu.cn/simple"
set "PADDLE_MIRROR=https://www.paddlepaddle.org.cn/packages/stable/cu118/"

:: ������Ļ
cls
echo ==================================================
echo      һ����װ Python �����廪���� + Paddle �ٷ�Դ��
echo ==================================================
echo.

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

:: ��ʾ��ǰ Python �汾
echo.
echo ��ǰ���� Python �汾��
python --version
echo.

:: ��ʾ GPU ����Ҫ��
echo [ע��] ��װ paddlepaddle-gpu ��Ҫ��
echo - NVIDIA GPU
echo - CUDA 11.8 �� cuDNN ����ȷ����
echo ���δ����Ҫ�󣬿��ܵ��°�װ������ʧ�ܡ�
echo.

:: ���� pip��ʹ���廪Դ��
echo �������� pip ...
pip install --upgrade pip -i %TSINGHUA_MIRROR%
if %ERRORLEVEL% neq 0 (
    echo [����] ���� pip ʧ�ܣ����������԰�װ��...
)

:: ��װ paddlepaddle-gpu
echo ���ڰ�װ paddlepaddle-gpu==3.0.0 ...
pip install paddlepaddle-gpu==3.0.0 -i %PADDLE_MIRROR%
if %ERRORLEVEL% neq 0 (
    echo [����] ��װ paddlepaddle-gpu ʧ�ܣ����� GPU ���������硣
    set "INSTALL_FAILED=1"
) else (
    echo [�ɹ�] paddlepaddle-gpu ��װ��ɡ�
)

:: ��װ flask
echo ���ڰ�װ flask ...
pip install flask -i %TSINGHUA_MIRROR%
if %ERRORLEVEL% neq 0 (
    echo [����] ��װ flask ʧ�ܣ�
    set "INSTALL_FAILED=1"
) else (
    echo [�ɹ�] flask ��װ��ɡ�
)

:: ��װ opencv-python
echo ���ڰ�װ opencv-python ...
pip install opencv-python -i %TSINGHUA_MIRROR%
if %ERRORLEVEL% neq 0 (
    echo [����] ��װ opencv-python ʧ�ܣ�
    set "INSTALL_FAILED=1"
) else (
    echo [�ɹ�] opencv-python ��װ��ɡ�
)

:: ��װ paddleocr
echo ���ڰ�װ paddleocr ...
pip install paddleocr -i %TSINGHUA_MIRROR%
if %ERRORLEVEL% neq 0 (
    echo [����] ��װ paddleocr ʧ�ܣ�
    set "INSTALL_FAILED=1"
) else (
    echo [�ɹ�] paddleocr ��װ��ɡ�
)

:: ��鰲װ���
echo.
echo ==================================================
if defined INSTALL_FAILED (
    echo      ��װ�����г��ִ���
    echo �������硢GPU ��������Դ�Ƿ���á�
) else (
    echo      ���а���װ�ɹ���
)
echo ==================================================
echo.

:: ��ʾ�Ѱ�װ��
echo ��ǰ�����Ѱ�װ�İ���
pip list
echo.

:: ��ʾʹ�÷���
echo ʹ�÷�����
echo 1. ˫�� "%WORK_DIR%python_env\activate_env.bat" �������
echo 2. ���������м���ʹ�� Python ���Ѱ�װ�İ����� flask��opencv-python��paddleocr����
echo 3. ���� "exit" �˳�������
echo.
echo ע�⣺���� paddlepaddle-gpu �� paddleocr ��Ҫ���ݵ� GPU ������
echo.
echo ��������˳�...
pause >nul

:: �˳����⻷��
deactivate
exit /b 0