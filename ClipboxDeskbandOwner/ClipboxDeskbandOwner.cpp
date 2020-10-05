// ClipboxDeskbandOwner.cpp : Defines the entry point for the application.
//

#include "framework.h"
#include "ClipboxDeskbandOwner.h"

#include <shlobj.h> 

HINSTANCE           g_hInstance;
BOOL                LoadDeskband();

int APIENTRY wWinMain(_In_ HINSTANCE hInstance,
                     _In_opt_ HINSTANCE hPrevInstance,
                     _In_ LPWSTR    lpCmdLine,
                     _In_ int       nCmdShow)
{
    UNREFERENCED_PARAMETER(hPrevInstance);
    UNREFERENCED_PARAMETER(lpCmdLine);

    g_hInstance = hInstance;

    if (!LoadDeskband())
    {
        return FALSE;
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


BOOL LoadDeskband()
{
   CoInitialize(nullptr);
   IDeskBand2* pDeskbandInterface = NULL;

   CLSID CLSID_ClipboxDeskBand =
   { 0x3c6d5961, 0xe6c7, 0x44f6,{ 0xa5, 0x22, 0xa2, 0xd, 0xe3, 0x3d, 0x2d, 0xac } };

   HRESULT  hResult = CoCreateInstance(CLSID_ClipboxDeskBand, NULL, CLSCTX_INPROC_SERVER, IID_PPV_ARGS(&pDeskbandInterface));
   
   if (SUCCEEDED(hResult))
    {        
        IBandSite* pBandSite = NULL;

        hResult = CoCreateInstance(CLSID_TrayBandSiteService, NULL, CLSCTX_ALL, IID_PPV_ARGS(&pBandSite));
        if (SUCCEEDED(hResult))
        {
            pBandSite->AddBand(pDeskbandInterface);
            pBandSite->Release();
        }
        pDeskbandInterface->Release();
    }

   return TRUE;
}
