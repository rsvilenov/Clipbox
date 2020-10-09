// ClipboxDeskbandOwner.cpp : Defines the entry point for the application.
//

#include "framework.h"

#include <shlobj.h> 
#include <tlhelp32.h>
#include <map>
#include <vector>

#include "ClipboxDeskbandOwner.h"

#include "../ClipboxDeskband.Infrastructure/ClipboxProductConstants.h"
#pragma comment(lib, "ClipboxDeskband.Infrastructure.lib")

static const UINT_PTR WAIT_SHELL_START_TIMER_ID = 1;
const int CHECK_SHELL_INTERVAL = 1000;

HINSTANCE           g_hInstance;
HWND                g_msgWindowHwnd;
BOOL                IsDeskbandShown();
BOOL                LoadDeskband(); 
BOOL CreateMsgWindow();
bool RegisterWaitEventsForExplorerProcesses(int sessionId);
bool RegisterWaitEventsForShellProcess();

struct SHELL_WND_DATA 
{
    HANDLE waitHandle;
    DWORD processId;
    HANDLE processHandle;
};


std::map<int, SHELL_WND_DATA*> g_explorerDataMapForCurrentSession;

int APIENTRY wWinMain(_In_ HINSTANCE hInstance,
                     _In_opt_ HINSTANCE hPrevInstance,
                     _In_ LPWSTR    lpCmdLine,
                     _In_ int       nCmdShow)
{
    UNREFERENCED_PARAMETER(hPrevInstance);
    UNREFERENCED_PARAMETER(lpCmdLine);

    g_hInstance = hInstance;

    if (!CreateMsgWindow())
    {
        return FALSE;
    }

    if (!RegisterWaitEventsForShellProcess())
    {
        SetTimer(g_msgWindowHwnd, WAIT_SHELL_START_TIMER_ID, CHECK_SHELL_INTERVAL, NULL);
    }
    else if (!IsDeskbandShown())
    {
        LoadDeskband();
    }

    HACCEL hAccelTable = LoadAccelerators(hInstance, MAKEINTRESOURCE(IDC_CLIPBOXDESKBANDOWNER));

    MSG msg;

    // Main message loop:
    while (GetMessage(&msg, nullptr, 0, 0))
    {
        if (!TranslateAccelerator(msg.hwnd, hAccelTable, &msg))
        {
            TranslateMessage(&msg);
            DispatchMessage(&msg);
        }
    }

    return (int) msg.wParam;
}

BOOL CreateMsgWindow()
{
    WNDCLASSW wc{};
    wc.hInstance = g_hInstance;
    wc.hCursor = LoadCursor(nullptr, IDC_ARROW);
    wc.hbrBackground = reinterpret_cast<HBRUSH>(GetStockObject(WHITE_BRUSH));
    wc.lpszClassName = L"ClipboxDeskbandHelperWindowClass";
    wc.cbWndExtra = sizeof(LONG_PTR);
    wc.lpfnWndProc = [](HWND hWnd, UINT message, WPARAM wParam, LPARAM lParam)
    {
        switch (message)
        {
            case WM_TIMER:
            {
                if (wParam == WAIT_SHELL_START_TIMER_ID)
                {
                    if (IsDeskbandShown()
                        || (RegisterWaitEventsForShellProcess()
                             && LoadDeskband()))
                    {
                        KillTimer(hWnd, WAIT_SHELL_START_TIMER_ID);
                    }
                }

                break;
            }
            case WM_APP:
            {
                SHELL_WND_DATA* shData = (SHELL_WND_DATA*)wParam;
                // UnregisterWait shouldn't be called directly 
                // from within RegisterWaitForSingleObject's callback
                UnregisterWait(shData->waitHandle);
                // we should not close the process handle before the wait is finished,
                // otherwise some strange things will start to happen...
                CloseHandle(shData->processHandle);
                g_explorerDataMapForCurrentSession.erase(shData->processId);
                if (g_explorerDataMapForCurrentSession.size() == 0)
                {
                    SetTimer(g_msgWindowHwnd, WAIT_SHELL_START_TIMER_ID, CHECK_SHELL_INTERVAL, NULL);
                }

                break;
            }
            case WM_DESTROY:
                PostQuitMessage(0);
                break;
        }
        return DefWindowProc(hWnd, message, wParam, lParam);
    };

    RegisterClassW(&wc);

    g_msgWindowHwnd = CreateWindowExW(0, wc.lpszClassName, L"ClipboxDeskbandHelperWindow", 0,
        CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT, HWND_MESSAGE, nullptr, g_hInstance, nullptr);
    
    return g_msgWindowHwnd != nullptr;
}


BOOL IsDeskbandShown()
{
    BOOL success = FALSE;

    CoInitialize(nullptr);
    ITrayDeskBand* pTrayDeskBand = NULL;
    HRESULT hr = CoCreateInstance(CLSID_TrayDeskBand, NULL, CLSCTX_ALL, IID_PPV_ARGS(&pTrayDeskBand));

    if (SUCCEEDED(hr))
    {
        hr = pTrayDeskBand->IsDeskBandShown(CLSID_ClipboxDeskBand);
        if (SUCCEEDED(hr) && (S_OK == hr))
        {
            success = true;
        }

        pTrayDeskBand->Release();
    }

    return success;
}

BOOL LoadDeskband()
{
   CoInitialize(nullptr);

   IDeskBand2* pDeskbandInterface = NULL;

   HRESULT hr = S_OK;

   // CLSID_ClipboxDeskBand is defined in ClipboxProductConstants.h (ClipboxDeskband.Infrastructure)
   hr = CoCreateInstance(CLSID_ClipboxDeskBand, NULL, CLSCTX_INPROC_SERVER, IID_PPV_ARGS(&pDeskbandInterface));
   if (SUCCEEDED(hr))
   {        
        IBandSite* pBandSite = NULL;

        hr = CoCreateInstance(CLSID_TrayBandSiteService, NULL, CLSCTX_ALL, IID_PPV_ARGS(&pBandSite));
        if (SUCCEEDED(hr))
        {
            hr = pBandSite->AddBand(pDeskbandInterface);
            pBandSite->Release();
        }
        pDeskbandInterface->Release();
    }

   return hr == S_OK;
}

static void CALLBACK OnExited(void* context, BOOLEAN isTimeOut)
{
    SHELL_WND_DATA* shData = g_explorerDataMapForCurrentSession[reinterpret_cast<int>(context)];
    PostMessage(g_msgWindowHwnd, WM_APP, (WPARAM)shData, 0);
}


bool RegisterWaitEventsForShellProcess()
{
    HWND hWndDesktop = GetDesktopWindow();

    HWND hShellTray = FindWindowEx(hWndDesktop, NULL, _T("Shell_TrayWnd"), NULL);
    HWND hReBar = FindWindowEx(hShellTray, NULL, _T("ReBarWindow32"), NULL);

    if (hShellTray != nullptr && hReBar != nullptr)
    {
        DWORD processID;
        GetWindowThreadProcessId(hReBar, &processID);

        HANDLE hProc = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ | SYNCHRONIZE, FALSE, processID);
        if (hProc)
        {
            HANDLE hWait;
            if (RegisterWaitForSingleObject(&hWait, hProc, &OnExited, reinterpret_cast<void*>(processID), INFINITE, WT_EXECUTEONLYONCE))
            {
                g_explorerDataMapForCurrentSession[processID] = new SHELL_WND_DATA
                {
                    hWait,
                    processID,
                    hProc
                };

                return TRUE;
            }
        }
        else
        {
            CloseHandle(hProc);
        }
    }
    else
    {

        DWORD currentSessionId;

        return ProcessIdToSessionId(GetCurrentProcessId(), &currentSessionId) && 
            RegisterWaitEventsForExplorerProcesses(currentSessionId);
    }
    

    return FALSE;
}

bool RegisterWaitEventsForExplorerProcesses(int sessionId)
{
    DWORD pid = 0;

    g_explorerDataMapForCurrentSession = std::map<int, SHELL_WND_DATA*>();

    // Create toolhelp snapshot.
    HANDLE snapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
    PROCESSENTRY32 process;
    ZeroMemory(&process, sizeof(process));
    process.dwSize = sizeof(process);

    // Walkthrough all processes.
    if (Process32First(snapshot, &process))
    {
        do
        {
            DWORD sessionId;
            if (ProcessIdToSessionId(process.th32ProcessID, &sessionId) == sessionId
                && _wcsicmp(process.szExeFile, L"explorer.exe") == 0)
            {
                // the handle, returned by OpenProcess is closed by the WM_APP message handler,
                // when the wait handle for the process is also closed.
                // It needs to be open in order for RegisterWaitForSingleObject to work properly.
                HANDLE hProcHandle = OpenProcess(SYNCHRONIZE, FALSE, process.th32ProcessID);
                if (hProcHandle)
                {
                    HANDLE hWaitHandle;
                    if (RegisterWaitForSingleObject(&hWaitHandle, hProcHandle, &OnExited, reinterpret_cast<void*>(process.th32ProcessID), INFINITE, WT_EXECUTEONLYONCE))
                    {
                        g_explorerDataMapForCurrentSession[process.th32ProcessID] = new SHELL_WND_DATA
                        {
                            hWaitHandle,
                            process.th32ProcessID,
                            hProcHandle
                        };
                    }
                }
            }
        } while (Process32Next(snapshot, &process));
    }

    CloseHandle(snapshot);

    return g_explorerDataMapForCurrentSession.size() > 0;
}
