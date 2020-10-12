#include <windows.h>
#include <uxtheme.h>
#include <strsafe.h>
#include "DeskBand.h"
#include "resource.h"
#include "ToolTip.h"

#include "shlwapi.h"

#include "../ClipboxDeskband.Infrastructure/Logger.h"
#include "../ClipboxDeskband.Infrastructure/NamedPipeClient.h"
#include "../ClipboxDeskband.Infrastructure/ClipboxProductConstants.h"
#pragma comment(lib, "ClipboxDeskband.Infrastructure.lib")

// GetDpiForMonitor 
#include "shellscalingapi.h"

#pragma comment(lib, "shlwapi.lib")

// for shellscalingapi.h
#pragma comment(lib, "shcore.lib")

#define RECTWIDTH(x)   ((x).right - (x).left)
#define RECTHEIGHT(x)  ((x).bottom - (x).top)

extern long        g_cDllRef;
extern HINSTANCE    g_hInst;

static const WCHAR g_szDeskBandClass[] = L"ClipboxDeskBandClass";

static const UINT_PTR g_TickTimerID = 1;
static const UINT_PTR g_ToolTipDelayTimerID = 2;

const int CHECK_ENABLED_INTERVAL = 1000;
const int TOOLTIP_DELAY_MS = 500;


const char* CMD_LEFT_CLICK = "LC";
const char* CMD_RIGHT_CLICK = "RC";

LPCWSTR APPDATA_SUBFOLDER = PRODCONST_ProductName;
LPCWSTR TOOLTIP_TEXT = PRODCONST_ProductName;

BOOL g_mouseOver = false;
BOOL g_fMouseTracking = false;
BOOL g_isClipboxDisabled = false;
BOOL g_isClipboxPaused = false;

HWND g_ToolTipHwnd;
TOOLINFOW g_ToolInfo;

ToolTip g_toolTip;
Logger* g_Logger;

UINT g_dpi = 96;


CDeskBand::CDeskBand() :
	m_cRef(1), m_pSite(NULL), m_fHasFocus(FALSE), m_fIsDirty(FALSE), m_dwBandID(0), m_hwnd(NULL), m_hwndParent(NULL)
{
	InterlockedIncrement(&g_cDllRef);

	InitializeLogger();
	if (g_Logger)
	{
		g_Logger->LogText("Started.");
	}
}

CDeskBand::~CDeskBand()
{
    if (m_pSite)
    {
        m_pSite->Release();
    }

	InterlockedDecrement(&g_cDllRef);

	delete g_Logger;
}

//
// IUnknown
//
STDMETHODIMP CDeskBand::QueryInterface(REFIID riid, void **ppv)
{
    HRESULT hr = S_OK;

    if (IsEqualIID(IID_IUnknown, riid)       ||
        IsEqualIID(IID_IOleWindow, riid)     ||
        IsEqualIID(IID_IDockingWindow, riid) ||
        IsEqualIID(IID_IDeskBand, riid)      ||
        IsEqualIID(IID_IDeskBand2, riid))
    {
        *ppv = static_cast<IOleWindow *>(this);
    }
    else if (IsEqualIID(IID_IPersist, riid) ||
             IsEqualIID(IID_IPersistStream, riid))
    {
        *ppv = static_cast<IPersist *>(this);
    }
    else if (IsEqualIID(IID_IObjectWithSite, riid))
    {
        *ppv = static_cast<IObjectWithSite *>(this);
    }
    else if (IsEqualIID(IID_IInputObject, riid))
    {
        *ppv = static_cast<IInputObject *>(this);
    }
    else
    {
        hr = E_NOINTERFACE;
        *ppv = NULL;
    }

    if (*ppv)
    {
        AddRef();
    }

    return hr;
}

STDMETHODIMP_(ULONG) CDeskBand::AddRef()
{
    return InterlockedIncrement(&m_cRef);
}

STDMETHODIMP_(ULONG) CDeskBand::Release()
{
    ULONG cRef = InterlockedDecrement(&m_cRef);
    if (0 == cRef)
    {
        delete this;
    }

    return cRef;
}

//
// IOleWindow
//
STDMETHODIMP CDeskBand::GetWindow(HWND *phwnd)
{
    *phwnd = m_hwnd;
    return S_OK;
}

STDMETHODIMP CDeskBand::ContextSensitiveHelp(BOOL)
{
    return E_NOTIMPL;
}

//
// IDockingWindow
//
STDMETHODIMP CDeskBand::ShowDW(BOOL fShow)
{
    if (m_hwnd)
    {
        ShowWindow(m_hwnd, fShow ? SW_SHOW : SW_HIDE);
    }

    return S_OK;
}

STDMETHODIMP CDeskBand::CloseDW(DWORD)
{
    if (m_hwnd)
    {
        ShowWindow(m_hwnd, SW_HIDE);
        DestroyWindow(m_hwnd);
        m_hwnd = NULL;
    }

    return S_OK;
}

STDMETHODIMP CDeskBand::ResizeBorderDW(const RECT *, IUnknown *, BOOL)
{
    return E_NOTIMPL;
}

//
// IDeskBand
//
STDMETHODIMP CDeskBand::GetBandInfo(DWORD dwBandID, DWORD, DESKBANDINFO *pdbi)
{
    HRESULT hr = E_INVALIDARG;

    if (pdbi)
    {
        m_dwBandID = dwBandID;

        if (pdbi->dwMask & DBIM_MINSIZE)
        {
            pdbi->ptMinSize.x = 40;
            pdbi->ptMinSize.y = 40;
        }

        if (pdbi->dwMask & DBIM_MAXSIZE)
        {
            pdbi->ptMaxSize.y = -1;
        }

        if (pdbi->dwMask & DBIM_INTEGRAL)
        {
            pdbi->ptIntegral.y = 1;
        }

        if (pdbi->dwMask & DBIM_ACTUAL)
        {
            pdbi->ptActual.x = 40;
            pdbi->ptActual.y = 40;
        }

        if (pdbi->dwMask & DBIM_TITLE)
        {
            // Don't show title by removing this flag.
			pdbi->dwMask &= ~DBIM_TITLE;
        }

        if (pdbi->dwMask & DBIM_MODEFLAGS)
        {
            pdbi->dwModeFlags = DBIMF_NOMARGINS  | DBIMF_NOGRIPPER | DBIMF_VARIABLEHEIGHT;
        }

        if (pdbi->dwMask & DBIM_BKCOLOR)
        {
            // Use the default background color by removing this flag.
            pdbi->dwMask &= ~DBIM_BKCOLOR;
        }

        hr = S_OK;
    }

    return hr;
}

//
// IDeskBand2
//
STDMETHODIMP CDeskBand::CanRenderComposited(BOOL *pfCanRenderComposited)
{
    *pfCanRenderComposited = TRUE;

    return S_OK;
}

STDMETHODIMP CDeskBand::SetCompositionState(BOOL fCompositionEnabled)
{
    m_fCompositionEnabled = fCompositionEnabled;

    InvalidateRect(m_hwnd, NULL, TRUE);
    UpdateWindow(m_hwnd);

    return S_OK;
}

STDMETHODIMP CDeskBand::GetCompositionState(BOOL *pfCompositionEnabled)
{
    *pfCompositionEnabled = m_fCompositionEnabled;

    return S_OK;
}

//
// IPersist
//
STDMETHODIMP CDeskBand::GetClassID(CLSID *pclsid)
{
    *pclsid = PRODCONST_CLSID_ClipboxDeskBand;
    return S_OK;
}

//
// IPersistStream
//
STDMETHODIMP CDeskBand::IsDirty()
{
    return m_fIsDirty ? S_OK : S_FALSE;
}

STDMETHODIMP CDeskBand::Load(IStream * /*pStm*/)
{
    return S_OK;
}

STDMETHODIMP CDeskBand::Save(IStream * /*pStm*/, BOOL fClearDirty)
{
    if (fClearDirty)
    {
        m_fIsDirty = FALSE;
    }

    return S_OK;
}

STDMETHODIMP CDeskBand::GetSizeMax(ULARGE_INTEGER * /*pcbSize*/)
{
    return E_NOTIMPL;
}

//
// IObjectWithSite
//
STDMETHODIMP CDeskBand::SetSite(IUnknown *pUnkSite)
{
    HRESULT hr = S_OK;

    m_hwndParent = NULL;

    if (m_pSite)
    {
        m_pSite->Release();
		// https://social.msdn.microsoft.com/Forums/windowsdesktop/en-US/3663de60-be1d-4a90-aca6-1b88b7a0a41a/windows-7-explorer-periodically-crashes-when-a-deskband-is-enabled?forum=windowssdk
		m_pSite = NULL;
    }

    if (pUnkSite)
    {
        IOleWindow *pow;
        hr = pUnkSite->QueryInterface(IID_IOleWindow, reinterpret_cast<void **>(&pow));
        if (SUCCEEDED(hr))
        {
            hr = pow->GetWindow(&m_hwndParent);
            if (SUCCEEDED(hr))
            {
                WNDCLASSW wc = { 0 };
                wc.style         = CS_HREDRAW | CS_VREDRAW;
                wc.hCursor       = LoadCursor(NULL, IDC_ARROW);
                wc.hInstance     = g_hInst;
                wc.lpfnWndProc   = WndProc;
                wc.lpszClassName = g_szDeskBandClass;
                wc.hbrBackground = CreateSolidBrush(RGB(255, 255, 0));

                RegisterClassW(&wc);

                CreateWindowExW(0,
                                g_szDeskBandClass,
                                NULL,
                                SS_BITMAP | WS_CHILD | WS_CLIPCHILDREN | WS_CLIPSIBLINGS,
                                0,
                                0,
                                0,
                                0,
                                m_hwndParent,
                                NULL,
                                g_hInst,
                                this);

                if (!m_hwnd)
                {
                    hr = E_FAIL;
                }
            }

            pow->Release();
        }

        hr = pUnkSite->QueryInterface(IID_IInputObjectSite, reinterpret_cast<void **>(&m_pSite));
    }

    return hr;
}

STDMETHODIMP CDeskBand::GetSite(REFIID riid, void **ppv)
{
    HRESULT hr = E_FAIL;

    if (m_pSite)
    {
        hr =  m_pSite->QueryInterface(riid, ppv);
    }
    else
    {
        *ppv = NULL;
    }

    return hr;
}

//
// IInputObject
//
STDMETHODIMP CDeskBand::UIActivateIO(BOOL fActivate, MSG *)
{
    if (fActivate)
    {
        SetFocus(m_hwnd);
    }

    return S_OK;
}

STDMETHODIMP CDeskBand::HasFocusIO()
{
    return m_fHasFocus ? S_OK : S_FALSE;
}

STDMETHODIMP CDeskBand::TranslateAcceleratorIO(MSG *)
{
    return S_FALSE;
};

void CDeskBand::OnFocus(const BOOL fFocus)
{
    m_fHasFocus = fFocus;

    if (m_pSite)
    {
        m_pSite->OnFocusChangeIS(static_cast<IOleWindow*>(this), m_fHasFocus);
    }
}

void CDeskBand::DrawIcons(const HDC hdc, const RECT area)
{
	// center the icon
	double scaleFactor = g_dpi / 96.0;
	int iconWidth = (int)(26 * scaleFactor);
	int iconHeight = (int)(26 * scaleFactor);
	int iconX = (RECTWIDTH(area) - iconWidth) / 2;
	int iconY = (RECTHEIGHT(area) - iconHeight) / 2;

	// draw the main icon
	HICON hIcon = (HICON) ::LoadImage(g_hInst, MAKEINTRESOURCE(IDI_ICON1), IMAGE_ICON, iconWidth, iconHeight, LR_SHARED | LR_LOADTRANSPARENT);
	if (!hIcon)
	{
		g_Logger->LogLastWin32Error();
	}
	else
	{
		if (!DrawIconEx(hdc, iconX, iconY, hIcon, iconWidth, iconHeight, 0, NULL, DI_NORMAL))
		{
			if (g_Logger)
				g_Logger->LogLastWin32Error();
		}

		DestroyIcon(hIcon);

		if (g_isClipboxPaused)
		{
			// draw the pause icon if the program is disabled
			HICON hPauseIcon = (HICON) ::LoadImage(g_hInst, MAKEINTRESOURCE(IDI_ICON3), IMAGE_ICON, iconWidth, iconHeight, LR_SHARED | LR_LOADTRANSPARENT);
			if (!hPauseIcon)
			{
				if (g_Logger)
					g_Logger->LogLastWin32Error();
			}
			else
			{
				if (!DrawIconEx(hdc, iconX + iconWidth / 2, iconY + iconHeight / 2, hPauseIcon, iconWidth / 2, iconHeight / 2, 0, NULL, DI_NORMAL))
				{
					if (g_Logger)
						g_Logger->LogLastWin32Error();
				}

				DestroyIcon(hPauseIcon);
			}
		}
		else if (g_isClipboxDisabled)
		{
			// draw the slash icon if the program is disabled
			HICON hSlashIcon = (HICON) ::LoadImage(g_hInst, MAKEINTRESOURCE(IDI_ICON2), IMAGE_ICON, iconWidth, iconHeight, LR_SHARED | LR_LOADTRANSPARENT);
			if (!hSlashIcon)
			{
				if (g_Logger)
					g_Logger->LogLastWin32Error();
			}
			else
			{
				if (!DrawIconEx(hdc, iconX, iconY, hSlashIcon, iconWidth, iconHeight, 0, NULL, DI_NORMAL))
				{
					if (g_Logger)
						g_Logger->LogLastWin32Error();
				}

				DestroyIcon(hSlashIcon);
			}
		}
	}
}


void CDeskBand::OnPaint(const HDC hdcIn)
{
    HDC hdc = hdcIn;
    PAINTSTRUCT ps;

    if (!hdc)
    {
        hdc = BeginPaint(m_hwnd, &ps);
    }

	if (hdc)
	{
		RECT rcWindow;
		GetClientRect(m_hwnd, &rcWindow);

		HBRUSH brush = CreateSolidBrush(RGB(0, 0, 0));
		if (g_mouseOver)
		{
			brush = CreateSolidBrush(RGB(33, 33, 33));
		}

		FillRect(hdc, &rcWindow, brush);

		DeleteObject(brush);

		DrawIcons(hdc, rcWindow);
	}

    if (!hdcIn)
    {
        EndPaint(m_hwnd, &ps);
    }
}

void CDeskBand::InitializeLogger()
{
	WCHAR szAppData[MAX_PATH];
	HRESULT hr = SHGetFolderPath(NULL, CSIDL_APPDATA, NULL, 0, szAppData);
	if (SUCCEEDED(hr))
	{
		PathAppendW(szAppData, APPDATA_SUBFOLDER);
		PathAppendW(szAppData, L"deskband.log");
		g_Logger = new Logger(szAppData);
	}
}

void CDeskBand::RefreshClipboxDisabledOrPausedState()
{
	g_isClipboxDisabled = CheckIfAppDataFileExists(L"disabled");
	g_isClipboxPaused = CheckIfAppDataFileExists(L"paused");
}

bool CDeskBand::CheckIfAppDataFileExists(LPCWSTR fileName)
{
	bool exists = false;
	WCHAR szAppData[MAX_PATH];

	HRESULT hr = SHGetFolderPath(NULL, CSIDL_APPDATA, NULL, 0, szAppData);
	if (SUCCEEDED(hr))
	{
		PathAppendW(szAppData, APPDATA_SUBFOLDER);
		PathAppendW(szAppData, fileName);
		DWORD dwAttrib = GetFileAttributes(szAppData);
		// if the file exists, Clipbox is disabled
		exists = ((dwAttrib != INVALID_FILE_ATTRIBUTES &&
			!(dwAttrib & FILE_ATTRIBUTE_DIRECTORY)));
	}

	return exists;
}

LRESULT CALLBACK CDeskBand::WndProc(HWND hwnd, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
	HRESULT hr;
    LRESULT lResult = 0;
	TRACKMOUSEEVENT tme;
	NamedPipeClient npClient;
	BOOL previousDisabledState = g_isClipboxDisabled;
	BOOL previousPausedState = g_isClipboxPaused;

	HMONITOR mon;
	UINT dpiX, dpiY;


    CDeskBand *pDeskBand = reinterpret_cast<CDeskBand *>(GetWindowLongPtr(hwnd, GWLP_USERDATA));
    switch (uMsg)
    {
    case WM_CREATE:
        pDeskBand = reinterpret_cast<CDeskBand *>(reinterpret_cast<CREATESTRUCT *>(lParam)->lpCreateParams);
        pDeskBand->m_hwnd = hwnd;
        SetWindowLongPtr(hwnd, GWLP_USERDATA, reinterpret_cast<LONG_PTR>(pDeskBand));

		mon = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
		
		if (GetDpiForMonitor(mon, MDT_EFFECTIVE_DPI, &dpiX, &dpiY) == S_OK)
		{
			g_dpi = dpiY;
		}

		g_toolTip.Initialize(hwnd, TOOLTIP_TEXT);

		SetTimer(hwnd, g_TickTimerID, CHECK_ENABLED_INTERVAL, NULL);

		break;

	case WM_DESTROY:
		KillTimer(hwnd, g_TickTimerID);
		break;

	case WM_TIMER:
		if (wParam == g_TickTimerID)
		{
			pDeskBand->RefreshClipboxDisabledOrPausedState();
			if (previousDisabledState != g_isClipboxDisabled
				|| previousPausedState != g_isClipboxPaused)
			{
				RedrawWindow(hwnd, NULL, NULL, RDW_INVALIDATE);
			}
		}
		else if (wParam == g_ToolTipDelayTimerID)
		{
			g_toolTip.Show();
			KillTimer(hwnd, g_ToolTipDelayTimerID);
		}
		break;

    case WM_SETFOCUS:
        pDeskBand->OnFocus(TRUE);
        break;

    case WM_KILLFOCUS:
        pDeskBand->OnFocus(FALSE);
        break;

    case WM_PAINT:
        pDeskBand->OnPaint(NULL);
        break;

    case WM_PRINTCLIENT:
        pDeskBand->OnPaint(reinterpret_cast<HDC>(wParam));
        break;

    case WM_ERASEBKGND:
        if (pDeskBand->m_fCompositionEnabled)
        {
            lResult = 1;
        }
        break;

	case WM_LBUTTONDOWN:
		// hide the tooltip and stop the tooltip timer
		// before showing the main screen
		g_toolTip.Hide();
		KillTimer(hwnd, g_ToolTipDelayTimerID);

		break;
	case WM_LBUTTONUP:
		hr = pDeskBand->SendFormatedPipeMessage(CMD_LEFT_CLICK, hwnd);
		if (g_Logger && FAILED(hr))
		{
			g_Logger->LogHR(hr);
		}
		break;
	case WM_RBUTTONDOWN:
		// hide the tooltip and stop the tooltip timer,
		// so that we can show the context menu
		g_toolTip.Hide();
		KillTimer(hwnd, g_ToolTipDelayTimerID);

		// disable the default taskbar context menu
		return 0;
		break;
	case WM_RBUTTONUP:
		hr = pDeskBand->SendFormatedPipeMessage(CMD_RIGHT_CLICK, hwnd);
		if (g_Logger && FAILED(hr))
		{
			g_Logger->LogHR(hr);
		}

		// disable the default taskbar context menu
		return 0;
		break;
	case  WM_MOUSEMOVE:
		if (!g_fMouseTracking)
		{
			// start tracking if we aren't already
			tme.cbSize = sizeof(TRACKMOUSEEVENT);
			tme.dwFlags = TME_HOVER | TME_LEAVE;
			tme.hwndTrack = pDeskBand->m_hwnd;
			tme.dwHoverTime = 1;
			g_fMouseTracking = TrackMouseEvent(&tme);
		}
		break;
	case WM_MOUSEHOVER:
		g_mouseOver = true;
		g_Logger->LogText("Mouse hover");
		// start the tooltip timer
		SetTimer(hwnd, g_ToolTipDelayTimerID, TOOLTIP_DELAY_MS, NULL);

		// OnPaint will highlight the icon if it sees that g_mouseOver is true
		RedrawWindow(hwnd, NULL, NULL, RDW_INVALIDATE);

		// set up leave tracking
		tme.cbSize = sizeof(TRACKMOUSEEVENT);
		tme.dwFlags = TME_LEAVE;
		tme.hwndTrack = pDeskBand->m_hwnd;
		g_fMouseTracking = TrackMouseEvent(&tme);
		break;
	case WM_MOUSELEAVE:
		g_mouseOver = false;
		g_Logger->LogText("Mouse Leave");
		// redraw the icon to its original state (unhighlighted)
		RedrawWindow(hwnd, NULL, NULL, RDW_INVALIDATE);

		// start tracking if we aren't already
		g_fMouseTracking = FALSE; // tracking now cancelled

		// hide the tooltip and stop the tooltip timer
		g_toolTip.Hide();
		KillTimer(hwnd, g_ToolTipDelayTimerID);
		break;

	// the deskband never receives this message...
	//case WM_DPICHANGED:
	//	g_dpi = HIWORD(wParam);

	// so in odrer to detect a DPI change,
	// I will have to use the general purpose message WM_SETTINGCHANGE
	case WM_SETTINGCHANGE:
		mon = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);

		if (GetDpiForMonitor(mon, MDT_EFFECTIVE_DPI, &dpiX, &dpiY) == S_OK
			&& g_dpi != dpiY)
		{
			g_dpi = dpiY;
			RedrawWindow(hwnd, NULL, NULL, RDW_INVALIDATE);
		}
		
		break;
    }

    if (uMsg != WM_ERASEBKGND)
    {
        lResult = DefWindowProc(hwnd, uMsg, wParam, lParam);
    }

    return lResult;
}

HRESULT CDeskBand::SendFormatedPipeMessage(IN const char* command, HWND hWnd)
{
	HRESULT hr = S_OK;
	NamedPipeClient npClient;

	size_t size = strlen(command) + 6 /* hWnd: */ + 10 /* int's max num of digits is 10 */;
	char* formattedMessage = (char*)malloc(size + 1 /* 1 for termianting \0 */);
	hr = StringCchPrintfA(formattedMessage, 
		size,
		"%s hWnd:%d", command, hWnd);

	if (SUCCEEDED(hr))
	{
		hr = npClient.SendPipeMessage(formattedMessage);
	}

	return hr;
}