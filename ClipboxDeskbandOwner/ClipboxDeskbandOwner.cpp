// ClipboxDeskbandOwner.cpp : Defines the entry point for the application.
//

#include "framework.h"

#include "shlwapi.h"
#pragma comment(lib, "shlwapi.lib")

#include <shlobj.h> 
#include <tlhelp32.h>
#include <map>
#include <vector>

#include "ClipboxDeskbandOwner.h"

#include "../ClipboxDeskband.Infrastructure/ClipboxProductConstants.h"
#include "../ClipboxDeskband.Infrastructure/Logger.h"
#pragma comment(lib, "ClipboxDeskband.Infrastructure.lib")

static const UINT_PTR WAIT_SHELL_START_TIMER_ID = 1;
const int CHECK_SHELL_INTERVAL = 1000;
LPCWSTR APPDATA_SUBFOLDER = L"Clipbox";

struct SHELL_WND_DATA
{
    HANDLE waitHandle;
    DWORD processId;
    HANDLE processHandle;
};

HINSTANCE           g_hInstance;
HWND                g_msgWindowHwnd;
Logger*              g_logger;
std::map<int, SHELL_WND_DATA*> g_explorerDataMapForCurrentSession;

void                InitializeLogger();
BOOL                IsDeskbandShown();
BOOL                LoadDeskband(); 
BOOL                CreateMsgWindow();
BOOL                RegisterWaitEventsForExplorerProcesses(int sessionId);
BOOL                RegisterWaitEventsForShellProcess();
BOOL                TryRemoveDeskband();
BOOL                ShowDeskband();

int APIENTRY wWinMain(_In_ HINSTANCE hInstance,
                     _In_opt_ HINSTANCE hPrevInstance,
                     _In_ LPWSTR    lpCmdLine,
                     _In_ int       nCmdShow)
{
    UNREFERENCED_PARAMETER(hPrevInstance);
    UNREFERENCED_PARAMETER(lpCmdLine);

    g_hInstance = hInstance;

    InitializeLogger();

    if (!CreateMsgWindow())
    {
        return FALSE;
    }

    if (!RegisterWaitEventsForShellProcess())
    {
        SetTimer(g_msgWindowHwnd, WAIT_SHELL_START_TIMER_ID, CHECK_SHELL_INTERVAL, NULL);
    }
    else
    {
        TryRemoveDeskband();
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

void InitializeLogger()
{
    WCHAR szAppData[MAX_PATH];
    HRESULT hr = SHGetFolderPath(NULL, CSIDL_APPDATA, NULL, 0, szAppData);
    if (SUCCEEDED(hr))
    {
        PathAppendW(szAppData, APPDATA_SUBFOLDER);
        PathAppendW(szAppData, L"deskbandOwner.log");
        g_logger = new Logger(szAppData);
    }
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
                    TryRemoveDeskband();
                    if (RegisterWaitEventsForShellProcess()
                             && LoadDeskband())
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
        hr = pTrayDeskBand->IsDeskBandShown(PRODCONST_CLSID_ClipboxDeskBand);
        if (SUCCEEDED(hr) && (S_OK == hr))
        {
            success = true;
        }

        pTrayDeskBand->Release();
    }

    CoUninitialize();

    return success;
}

BOOL ShowDeskband()
{
    BOOL success = FALSE;

    CoInitialize(nullptr);
    ITrayDeskBand* pTrayDeskBand = NULL;
    HRESULT hr = CoCreateInstance(CLSID_TrayDeskBand, NULL, CLSCTX_ALL, IID_PPV_ARGS(&pTrayDeskBand));

    if (SUCCEEDED(hr))
    {
        hr = pTrayDeskBand->ShowDeskBand(PRODCONST_CLSID_ClipboxDeskBand);
        if (SUCCEEDED(hr) && (S_OK == hr))
        {
            success = true;
        }

        pTrayDeskBand->Release();
    }

    CoUninitialize();

    return success;
}


BOOL TryRemoveDeskband()
{
    BOOL success = FALSE;

    CoInitialize(NULL);
    HRESULT hr;

    IBandSite* pBandSite;
    hr = CoCreateInstance(CLSID_TrayBandSiteService, NULL, CLSCTX_SERVER, IID_IBandSite, (LPVOID*)&pBandSite);

    if (SUCCEEDED(hr))
    {
        HRESULT hr;

        IDeskBand* pBand = 0;
        DWORD bandID = 0;

        int i = 0;
        while (SUCCEEDED(pBandSite->EnumBands(i, &bandID)))
        {
            i++;

            hr = pBandSite->GetBandObject(bandID, IID_IDeskBand, (void**)&pBand);
            if (FAILED(hr))
            {
                continue;
            }

            IPersist* pPersist = 0;
            if (SUCCEEDED(pBand->QueryInterface(IID_IPersist, (void**)&pPersist)))
            {
                CLSID clsid = CLSID_NULL;
                if (SUCCEEDED(pPersist->GetClassID(&clsid)))
                {
                    if (clsid == PRODCONST_CLSID_ClipboxDeskBand)
                    {
                        //HRESULT hr = pBandSite->SetBandState(bandID, BSSF_VISIBLE, UINT_MAX);
                        HRESULT hr = pBandSite->RemoveBand(bandID);
                        success = SUCCEEDED(hr);
                        //break;
                    }
                }
                pPersist->Release();

            }

            pBand->Release();
        }

        pBandSite->Release();
    }


    CoUninitialize();

    return success;
}

BOOL LoadDeskband()
{
    CoInitialize(nullptr);

   IDeskBand2* pDeskbandInterface = NULL;

   HRESULT hr = S_OK;

   // CLSID_ClipboxDeskBand is defined in ClipboxProductConstants.h (ClipboxDeskband.Infrastructure)
   hr = CoCreateInstance(PRODCONST_CLSID_ClipboxDeskBand, NULL, CLSCTX_INPROC_SERVER, IID_PPV_ARGS(&pDeskbandInterface));
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

   if (FAILED(hr) && hr != REGDB_E_CLASSNOTREG)
   {
       g_logger->LogHR(hr);
   }
   
   CoUninitialize();

   return SUCCEEDED(hr);
}

static void CALLBACK OnExited(void* context, BOOLEAN isTimeOut)
{
    SHELL_WND_DATA* shData = g_explorerDataMapForCurrentSession[reinterpret_cast<int>(context)];
    PostMessage(g_msgWindowHwnd, WM_APP, (WPARAM)shData, 0);
}


BOOL RegisterWaitEventsForShellProcess()
{
    HWND hWndDesktop = GetDesktopWindow();
    if (!hWndDesktop)
    {
        return FALSE;
    }

    HWND hShellTray = FindWindowEx(hWndDesktop, NULL, _T("Shell_TrayWnd"), NULL);
    HWND hReBar = FindWindowEx(hShellTray, NULL, _T("ReBarWindow32"), NULL);

    if (hShellTray && hReBar)
    {
        DWORD processID;
        GetWindowThreadProcessId(hReBar, &processID);

        if (g_explorerDataMapForCurrentSession.count(processID) > 0)
        {
            // the wait is already registered
            return TRUE;
        }

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
    /*else
    {
        g_logger->LogLastWin32Error();
        g_logger->LogText("Resorting to using RegisterWaitEventsForExplorerProcesses()");

        DWORD currentSessionId;

        return ProcessIdToSessionId(GetCurrentProcessId(), &currentSessionId) && 
            RegisterWaitEventsForExplorerProcesses(currentSessionId);
    }*/
    

    return FALSE;
}

BOOL RegisterWaitEventsForExplorerProcesses(int sessionId)
{
    DWORD pid = 0;

    g_explorerDataMapForCurrentSession = std::map<int, SHELL_WND_DATA*>();

    // create a toolhelp snapshot
    HANDLE snapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
    if (!snapshot)
    {
        g_logger->LogLastWin32Error();
        g_logger->LogText("Failed to create a toolhelp snapshot");
        return FALSE;
    }

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
                else
                {
                    g_logger->LogLastWin32Error();
                    g_logger->LogText("Failed to open the explorer's process wihch hosts the taskbar (RegisterWaitEventsForExplorerProcesses).");
                }
            }
        } while (Process32Next(snapshot, &process));
    }

    CloseHandle(snapshot);

    return g_explorerDataMapForCurrentSession.size() > 0;
}
