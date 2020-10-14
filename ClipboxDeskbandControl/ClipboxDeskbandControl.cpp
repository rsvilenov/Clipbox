// ClipboxDeskbandControl.cpp : Defines the entry point for the application.
//

#include <shlwapi.h>
#pragma comment(lib, "shlwapi.lib")

#include <ShlObj.h>

#include "framework.h"
#include "ClipboxDeskbandControl.h"

#include "../ClipboxDeskband.Infrastructure/ClipboxProductConstants.h"
#include "../ClipboxDeskband.Infrastructure/DeskbandController.h"
#include "../ClipboxDeskband.Infrastructure/Logger.h"
#pragma comment(lib, "ClipboxDeskband.Infrastructure.lib")


LPCWSTR APPDATA_SUBFOLDER = L"Clipbox";
LPCWSTR ARG_NOWAIT = L"nowait";

static const UINT_PTR WAIT_CONFIRM_DIALOG_TIMER_ID = 1;
static const UINT_PTR WAIT_CONFIRM_DIALOG_ANSWERED_TIMER_ID = 2;
static const UINT_PTR WAIT_DESKBAND_SHOW_TIMER_ID = 3;
const int TIMERS_INTERVAL = 1000;
const int WAIT_CONFIRM_DIALOG_TIMER_STOP_AFTER = 5 * TIMERS_INTERVAL;
const int WAIT_CONFIRM_DIALOG_ANSWERED_TIMER_STOP_AFTER = 5 * TIMERS_INTERVAL;
const int WAIT_DESKBAND_SHOW_TIMER_STOP_AFTER = 10 * TIMERS_INTERVAL;

int ConfirmDialogTimerPassedSeconds;
int ConfirmDialogAnsweredTimerPassedSeconds;
int DeskbandShowTimerPassedSeconds;
bool g_confirmDialogHasAlreadbyBeenShown;

enum class EXIT_CODES
{
    OK,
    DESKBAND_NOT_REGISTERED,
    DESKBAND_FAILED_TO_SHOW,
    UNKNOWN
};

HINSTANCE           g_hInstance;
HWND                g_msgWindowHwnd;
Logger*             g_logger;
DeskbandController* g_deskbandController;
bool                g_nowait;

BOOL                CreateMsgWindow();
void                InitializeLogger();

int APIENTRY wWinMain(_In_ HINSTANCE hInstance,
                     _In_opt_ HINSTANCE hPrevInstance,
                     _In_ LPWSTR    lpCmdLine,
                     _In_ int       nCmdShow)
{
    UNREFERENCED_PARAMETER(hPrevInstance);
    UNREFERENCED_PARAMETER(lpCmdLine);
    g_hInstance = hInstance;

    InitializeLogger();
    g_deskbandController = new DeskbandController(PRODCONST_CLSID_ClipboxDeskBand);
    HRESULT hr = g_deskbandController->Initialize();
    if (FAILED(hr))
    {
        g_logger->LogHR(hr);
        return (int)EXIT_CODES::UNKNOWN;
    }

    g_nowait = wcscmp(lpCmdLine, ARG_NOWAIT) == 0;

    if (!CreateMsgWindow())
    {
        g_logger->LogText("Failed to create main window");
        return (int)EXIT_CODES::UNKNOWN;
    }

    HACCEL hAccelTable = LoadAccelerators(hInstance, MAKEINTRESOURCE(IDC_CLIPBOXDESKBANDCONTROL));

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
        PathAppendW(szAppData, L"deskbandControl.log");
        g_logger = new Logger(szAppData);
    }
}

BOOL CreateMsgWindow()
{
    WNDCLASSW wc{};
    wc.hInstance = g_hInstance;
    wc.hCursor = LoadCursor(nullptr, IDC_ARROW);
    wc.hbrBackground = reinterpret_cast<HBRUSH>(GetStockObject(WHITE_BRUSH));
    wc.lpszClassName = L"ClipboxDeskbandControlWindowClass";
    wc.cbWndExtra = sizeof(LONG_PTR);
    wc.lpfnWndProc = [](HWND hWnd, UINT message, WPARAM wParam, LPARAM lParam)
    {
        HRESULT hr;

        switch (message)
        {
        case WM_CREATE:
            // The deskband should already been registered by the installer.
            // Tell Windows Explorer to reload its shell extensions...
            SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, NULL, NULL);

            // try to show the deskband
            hr = g_deskbandController->ShowDeskband();
            if (SUCCEEDED(hr))
            {
                // wait for the confirimation dialog, shown by the shell (Windows Explorer)
                // to appear...
                SetTimer(hWnd, WAIT_CONFIRM_DIALOG_TIMER_ID, g_nowait ? 1 : TIMERS_INTERVAL, NULL);
            }
            else
            {
                g_logger->LogHR(hr);

                if (hr == REGDB_E_CLASSNOTREG)
                {
                    PostQuitMessage((int)EXIT_CODES::DESKBAND_NOT_REGISTERED);
                }
                else
                {
                    PostQuitMessage((int)EXIT_CODES::UNKNOWN);
                }
            }
            break;
        case WM_TIMER:
        {
            if (wParam == WAIT_CONFIRM_DIALOG_TIMER_ID)
            {
                ConfirmDialogTimerPassedSeconds += TIMERS_INTERVAL;
                if (ConfirmDialogTimerPassedSeconds > WAIT_CONFIRM_DIALOG_TIMER_STOP_AFTER)
                {
                    KillTimer(hWnd, WAIT_CONFIRM_DIALOG_TIMER_ID);

                    bool isDeskbandShown;
                    hr = g_deskbandController->IsDeskbandShown(&isDeskbandShown);
                    if (SUCCEEDED(hr))
                    {
                        PostQuitMessage(isDeskbandShown ? (int)EXIT_CODES::OK : (int)EXIT_CODES::UNKNOWN);
                    }
                    else
                    {
                        g_logger->LogHR(hr);

                        if (hr == REGDB_E_CLASSNOTREG)
                        {
                            PostQuitMessage((int)EXIT_CODES::DESKBAND_NOT_REGISTERED);
                        }
                        else
                        {
                            PostQuitMessage((int)EXIT_CODES::UNKNOWN);
                        }
                    }

                    break;
                }


                if (!g_confirmDialogHasAlreadbyBeenShown)
                {
                    // try to find the confirmation dialog that Windows Explorer shows
                    // before showing the deskband in the taskbar.
                    HWND confirmDialogHwnd = FindWindow(L"#32770", PRODCONST_ProductName);
                    if (confirmDialogHwnd)
                    {
                        g_confirmDialogHasAlreadbyBeenShown = true;
                        // bring the window to front and make it stay there, so 
                        // the user can see it (sometimes it appears somewhere behind the other windows)
                        if (SetWindowPos(confirmDialogHwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE))
                        {
                            KillTimer(hWnd, WAIT_CONFIRM_DIALOG_TIMER_ID);
                            // wait for the user to answer the dialog
                            SetTimer(hWnd, WAIT_CONFIRM_DIALOG_ANSWERED_TIMER_ID, g_nowait ? 1 : TIMERS_INTERVAL, NULL);
                        }
                    }
                }
            }
            else if (wParam == WAIT_CONFIRM_DIALOG_ANSWERED_TIMER_ID)
            {
                ConfirmDialogAnsweredTimerPassedSeconds += TIMERS_INTERVAL;
                
                // check if the confirmation window is already there
                HWND confirmDialogHwnd = FindWindow(L"#32770", PRODCONST_ProductName);
                // if it is not, kill the timer and wait for the deskband to appear
                if (!confirmDialogHwnd)
                {
                    // if it isn't stop the timer and wait for the deskband to appear
                    KillTimer(hWnd, WAIT_CONFIRM_DIALOG_ANSWERED_TIMER_ID);
                    SetTimer(hWnd, WAIT_DESKBAND_SHOW_TIMER_ID, TIMERS_INTERVAL, NULL);
                }
                // if it is still there, check how much time we have waited or whether nowait flag has been set
                else if(ConfirmDialogAnsweredTimerPassedSeconds > WAIT_CONFIRM_DIALOG_ANSWERED_TIMER_STOP_AFTER
                    || g_nowait)
                {
                    // we have waited for the user's responce enough time (or the program
                    // is started with the "nowait" flag, which will make it not wait for the user's decision),
                    // we are assuming they want the bar, so let's try to press the confirmation dialog's button
                    PostMessage(confirmDialogHwnd, WM_KEYDOWN, (WPARAM)VK_RETURN, 0);
                    PostMessage(confirmDialogHwnd, WM_KEYUP, (WPARAM)VK_RETURN, 0);

                    // don't check whether this auto-click succeeded...
                    // just start the next timer. It will timeout if the window is still there
                    // and the deskband showing process will fail.
                    KillTimer(hWnd, WAIT_CONFIRM_DIALOG_ANSWERED_TIMER_ID);
                    SetTimer(hWnd, WAIT_DESKBAND_SHOW_TIMER_ID, TIMERS_INTERVAL, NULL);
                }
            }
            else if (wParam == WAIT_DESKBAND_SHOW_TIMER_ID)
            {
                ConfirmDialogTimerPassedSeconds += TIMERS_INTERVAL;

                bool isShown;
                hr = g_deskbandController->IsDeskbandShown(&isShown);

                if (SUCCEEDED(hr) && isShown)
                {
                    KillTimer(hWnd, WAIT_DESKBAND_SHOW_TIMER_ID);
                    g_deskbandController->NotiryDeskbandRegistrationChange();
                    PostQuitMessage((int)EXIT_CODES::OK);
                }
                else if (ConfirmDialogTimerPassedSeconds > WAIT_CONFIRM_DIALOG_TIMER_STOP_AFTER)
                {
                    // if the deskband has not shown up within the time frame,
                    // quit with the "unknown" exit code
                    KillTimer(hWnd, WAIT_DESKBAND_SHOW_TIMER_ID);
                    PostQuitMessage((int)EXIT_CODES::UNKNOWN);
                }
            }

            break;
        }
        case WM_DESTROY:
            PostQuitMessage((int)EXIT_CODES::OK);
            break;
        }
        return DefWindowProc(hWnd, message, wParam, lParam);
    };

    RegisterClassW(&wc);

    g_msgWindowHwnd = CreateWindowExW(0, wc.lpszClassName, L"ClipboxDeskbandControlWindow", 0,
        CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT, HWND_MESSAGE, nullptr, g_hInstance, nullptr);

    return g_msgWindowHwnd != nullptr;
}