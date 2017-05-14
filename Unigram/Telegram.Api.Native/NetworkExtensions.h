#pragma once
#include <Winsock2.h>
#include <iphlpapi.h>

#ifndef NDIS_IF_MAX_STRING_SIZE
#define NDIS_IF_MAX_STRING_SIZE 256 
#endif

#ifndef NETIOAPI_API
#define NETIOAPI_API NTSTATUS WINAPI
#endif 


typedef _Return_type_success_(return >= 0) LONG NTSTATUS;
typedef ULONG NET_IFINDEX, *PNET_IFINDEX;
typedef UINT16 NET_IFTYPE, *PNET_IFTYPE;


typedef enum _MIB_NOTIFICATION_TYPE
{
	MibParameterNotification = 0,
	MibAddInstance = 1,
	MibDeleteInstance = 2,
	MibInitialNotification = 3
} MIB_NOTIFICATION_TYPE, *PMIB_NOTIFICATION_TYPE;


typedef struct _MIB_IPINTERFACE_ROW {
	ADDRESS_FAMILY Family;
	NET_LUID InterfaceLuid;
	NET_IFINDEX InterfaceIndex;
	ULONG MaxReassemblySize;
	ULONG64 InterfaceIdentifier;
	ULONG MinRouterAdvertisementInterval;
	ULONG MaxRouterAdvertisementInterval;
	BOOLEAN AdvertisingEnabled;
	BOOLEAN ForwardingEnabled;
	BOOLEAN WeakHostSend;
	BOOLEAN WeakHostReceive;
	BOOLEAN UseAutomaticMetric;
	BOOLEAN UseNeighborUnreachabilityDetection;
	BOOLEAN ManagedAddressConfigurationSupported;
	BOOLEAN OtherStatefulConfigurationSupported;
	BOOLEAN AdvertiseDefaultRoute;
	NL_ROUTER_DISCOVERY_BEHAVIOR   RouterDiscoveryBehavior;
	ULONG DadTransmits;
	ULONG BaseReachableTime;
	ULONG RetransmitTime;
	ULONG PathMtuDiscoveryTimeout;
	NL_LINK_LOCAL_ADDRESS_BEHAVIOR LinkLocalAddressBehavior;
	ULONG LinkLocalAddressTimeout;
	ULONG ZoneIndices[ScopeLevelCount];
	ULONG SitePrefixLength;
	ULONG Metric;
	ULONG NlMtu;
	BOOLEAN Connected;
	BOOLEAN SupportsWakeUpPatterns;
	BOOLEAN SupportsNeighborDiscovery;
	BOOLEAN SupportsRouterDiscovery;
	ULONG ReachableTime;
	NL_INTERFACE_OFFLOAD_ROD TransmitOffload;
	NL_INTERFACE_OFFLOAD_ROD ReceiveOffload;
	BOOLEAN DisableDefaultRoutes;
} MIB_IPINTERFACE_ROW, *PMIB_IPINTERFACE_ROW;


typedef VOID(WINAPI *PIPINTERFACE_CHANGE_CALLBACK) (_In_ PVOID CallerContext, _In_ PMIB_IPINTERFACE_ROW Row OPTIONAL, _In_ MIB_NOTIFICATION_TYPE NotificationType);


NETIOAPI_API NotifyIpInterfaceChange(_In_ ADDRESS_FAMILY Family, _In_ PIPINTERFACE_CHANGE_CALLBACK Callback, _In_opt_ PVOID CallerContext, _In_ BOOLEAN InitialNotification, _Inout_ HANDLE* NotificationHandle);
NETIOAPI_API CancelMibChangeNotify2(_In_ HANDLE NotificationHandle);
NETIOAPI_API ConvertInterfaceLuidToName(_In_ const NET_LUID* InterfaceLuid, _Out_ PWSTR InterfaceName, _In_ SIZE_T Length);
NETIOAPI_API ConvertInterfaceGuidToLuid(_In_ const GUID* InterfaceGuid, _Out_ PNET_LUID InterfaceLuid);
NETIOAPI_API ConvertInterfaceLuidToIndex(_In_ const NET_LUID* InterfaceLuid, _Out_ PNET_IFINDEX InterfaceIndex);
NETIOAPI_API GetIpInterfaceEntry(_Inout_ PMIB_IPINTERFACE_ROW Row);
VOID WINAPI InitializeIpInterfaceEntry(_Inout_ PMIB_IPINTERFACE_ROW Row);
DWORD WINAPI GetBestInterfaceEx(_In_  struct sockaddr* pDestAddr, _Out_ PDWORD pdwBestIfIndex);
DWORD WINAPI GetPerAdapterInfo(_In_ ULONG IfIndex, _Out_ PIP_PER_ADAPTER_INFO pPerAdapterInfo, _In_ PULONG pOutBufLen);