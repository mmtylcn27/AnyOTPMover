// dllmain.cpp : DLL uygulamasının giriş noktasını tanımlar.
#include "pch.h"

#include <cstdio>
#include <Windows.h>
#include <WbemIdl.h>
#include <string>
#include <fstream>
#include <io.h>
#include <fcntl.h>
#include <iostream>

#include "detours/detours.h"

#ifdef _WIN64
#pragma comment(lib, "detours/detoursx64.lib")
#else
#pragma comment(lib, "detours/detours.lib")
#endif

const std::string bSetText = "SpoofInfo.txt";

std::wstring bSpoofedPNP = L"NULL";
unsigned int bSpoofedSignature = 0;
unsigned int bPatchFlag = false;

const unsigned long bPtr1 = 0x00872AE4;
const unsigned long bPtr2 = 0x00872899;
const unsigned char bPatch[4] = { 0x90, 0x90, 0x90, 0x90 };

typedef HRESULT(__stdcall* tGetWMI)(
    IWbemClassObject*,
    LPCWSTR,
    LONG,
    VARIANT*,
    CIMTYPE*,
    long*
    );

tGetWMI pGetWMI = NULL;

HRESULT __stdcall hookGetWMI(
    IWbemClassObject* pThis,
    LPCWSTR wszName,
    LONG lFlags,
    VARIANT* pVal,
    CIMTYPE* pType,
    long* plFlavor
)
{
    HRESULT bRet = pGetWMI(
        pThis,
        wszName,
        lFlags,
        pVal,
        pType,
        plFlavor
    );

    if (bRet >= WBEM_S_NO_ERROR)
    {
        std::wstring bStr = wszName;
     
        if (pVal)
        {
            if (bStr.find(L"SIgnature") != std::string::npos)
            {
                printf("Original: %u, Spoofed: %u\n", pVal->uintVal, bSpoofedSignature);
            	
                if (bSpoofedSignature == 0)
                    pVal->vt = VT_NULL;
                else
                    pVal->vt = VT_UINT;

                pVal->uintVal = bSpoofedSignature;
            }

            if (bStr.find(L"PNPDeviceID") != std::string::npos && pVal->vt == VT_BSTR)
            {
                printf("Original: %S, Spoofed: %ls\n", pVal->bstrVal, bSpoofedPNP.c_str());
                SysFreeString(pVal->bstrVal);
                pVal->bstrVal = SysAllocString(bSpoofedPNP.c_str());
            }
        }
    }
	
    return bRet;
}


void Main()
{
    printf("Read %s\n", bSetText.c_str());
    std::ifstream file(bSetText);

    std::string bPNP;
    std::string bSIG;
    std::string bPAT;
	
    if (std::getline(file, bPNP))
    {
        printf("Readed %s\n", bPNP.c_str());
        bSpoofedPNP = std::wstring(bPNP.begin(), bPNP.end());
    }
	
    if (std::getline(file, bSIG))
    {
        printf("Readed %s\n", bSIG.c_str());
        bSpoofedSignature = static_cast<unsigned int>(std::stoul(bSIG));
    }

    if (std::getline(file, bPAT))
    {
        bPatchFlag = static_cast<bool>(std::stoul(bPAT));
        printf("Readed %s\n", bPatchFlag ? "true" : "false");
    }

    printf("Hook Start\n");
	
    HMODULE hFastprox = NULL;

    while (!hFastprox)
    {
        hFastprox = GetModuleHandleA("fastprox.dll");

        if (!hFastprox)
            Sleep( 100);
    }

    if (hFastprox)
        pGetWMI = (tGetWMI)GetProcAddress(
            hFastprox,
#ifdef _WIN64
            "?Get@CWbemObject@@UEAAJPEBGJPEAUtagVARIANT@@PEAJ2@Z"
#else
            "?Get@CWbemObject@@UAGJPBGJPAUtagVARIANT@@PAJ2@Z"
#endif
        );

    if (pGetWMI)
    {
        DetourTransactionBegin();
        DetourUpdateThread(GetCurrentThread());
        DetourAttach(&(PVOID&)pGetWMI, hookGetWMI);

        ULONG bRet = DetourTransactionCommit();
    	
        if (bRet == NO_ERROR)
            printf("Hook Completed\n");
        else
            printf("Hook Not Completed Error: %d\n", bRet);  
    }
    else
        printf("Hook Not Completed\n");


    if (bPatchFlag)
    {
        printf("Patch Active\n");

        unsigned long dwOldProtect = PAGE_EXECUTE_READWRITE;

        if (VirtualProtect((PVOID)bPtr1, 4, dwOldProtect, &dwOldProtect))
        {
            memcpy((PVOID)bPtr1, bPatch, 4);
            VirtualProtect((PVOID)bPtr1, 4, dwOldProtect, &dwOldProtect);
        }

        dwOldProtect = PAGE_EXECUTE_READWRITE;

        if (VirtualProtect((PVOID)bPtr2, 4, dwOldProtect, &dwOldProtect))
        {
            memcpy((PVOID)bPtr2, bPatch, 4);
            VirtualProtect((PVOID)bPtr2, 4, dwOldProtect, &dwOldProtect);
        }

        printf("Patch Complete\n");
    }
    else
        printf("Patch Deactive\n");
	
}

void CreateConsole()
{
    //Create a console for this application
    AllocConsole();

    // Get STDOUT handle
    HANDLE ConsoleOutput = GetStdHandle(STD_OUTPUT_HANDLE);
    int SystemOutput = _open_osfhandle(intptr_t(ConsoleOutput), _O_TEXT);
    FILE* COutputHandle = _fdopen(SystemOutput, "w");

    // Get STDERR handle
    HANDLE ConsoleError = GetStdHandle(STD_ERROR_HANDLE);
    int SystemError = _open_osfhandle(intptr_t(ConsoleError), _O_TEXT);
    FILE* CErrorHandle = _fdopen(SystemError, "w");

    // Get STDIN handle
    HANDLE ConsoleInput = GetStdHandle(STD_INPUT_HANDLE);
    int SystemInput = _open_osfhandle(intptr_t(ConsoleInput), _O_TEXT);
    FILE* CInputHandle = _fdopen(SystemInput, "r");

    //make cout, wcout, cin, wcin, wcerr, cerr, wclog and clog point to console as well
    std::ios::sync_with_stdio(true);

    // Redirect the CRT standard input, output, and error handles to the console
    freopen_s(&CInputHandle, "CONIN$", "r", stdin);
    freopen_s(&COutputHandle, "CONOUT$", "w", stdout);
    freopen_s(&CErrorHandle, "CONOUT$", "w", stderr);

    //Clear the error state for each of the C++ standard stream objects. We need to do this, as
    //attempts to access the standard streams before they refer to a valid target will cause the
    //iostream objects to enter an error state. In versions of Visual Studio after 2005, this seems
    //to always occur during startup regardless of whether anything has been read from or written to
    //the console or not.
    std::wcout.clear();
    std::cout.clear();
    std::wcerr.clear();
    std::cerr.clear();
    std::wcin.clear();
    std::cin.clear();

}

BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved
                     )
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
    {
        DisableThreadLibraryCalls(hModule);
        CreateConsole();
        CreateThread(0, 0, (LPTHREAD_START_ROUTINE)Main, 0, 0, 0);
        break;
    }
    case DLL_PROCESS_DETACH:
    {
        if (pGetWMI)
        {
            DetourTransactionBegin();
            DetourUpdateThread(GetCurrentThread());
            DetourDetach(&(PVOID&)pGetWMI, hookGetWMI);
            DetourTransactionCommit();
        }
        break;
    }
    }
    return TRUE;
}

