#include <stdio.h>
#include "onvifAPI.h"



int main()
{
	int ret = 0, ch, s;
	printf("==================================\n");
	printf("=============hello onvif==========\n");
	printf("==================================\n\n");

#if 0
	//设备发现
	int devNum = 0, i;
	ONVIF_DEVICE_PROBE device[ONVIF_MAX_128]; 
	memset(device, 0, sizeof(ONVIF_DEVICE_PROBE) * ONVIF_MAX_128);

	ONVIF_DISCOVERY_Probe(3, ONVIF_MAX_128, device, &devNum);
	printf("Discovery device num: %d\n", devNum);
	for (i = 0; i < devNum; i++)
	{
		printf("device ip: %s port: %d location: %s devName: %s onvifUrl: %s\n", device[i].ip, device[i].port, device[i].location, device[i].devName, device[i].onvifUrl);
	}
#endif

	//获取能力集
	ONVIF_MANAGEMENT_CAPABILITIES capabilities;
	memset(&capabilities, 0 , sizeof(ONVIF_MANAGEMENT_CAPABILITIES));

	ONVIF_COMMON_INFO common;
	memset(&common, 0, sizeof(ONVIF_COMMON_INFO));

	ret = ONVIF_MAGEMENT_GetCapabilitiesEx(2, "192.168.83.155", 80, "admin", "rvssp100", &capabilities);
	if (!ret)
	{
		//完成公共结构填充，后续所有接口函数都需使用此结构体
		strcpy(common.username, "admin");
		strcpy(common.password, "rvssp200");
		memcpy(common.onvifUrls, capabilities.onvifUrls, sizeof(capabilities.onvifUrls));

		ONVIF_DEVICE_INFO device = { 0 };
		ret = ONVIF_MAGEMENT_GetDeviceInformation(2, &common, &device);

		ONVIF_MEDIA_CHANNEL_SOURCE channel;
		memset(&channel, 0, sizeof(ONVIF_MEDIA_CHANNEL_SOURCE));
		ret = ONVIF_MEDIA_GetChannelSource(2, &common, &channel);
		if (ret) return 0;

		for (ch = 0; ch < channel.sourceNum; ch++)
		{
			int streamNum = 0;
			ret = ONVIF_MEDIA_GetStreamNum(2, &common, channel.sourceToken[ch], &streamNum);

			for (s = 0; s < streamNum; s++)
			{
				//获取rtsp码流地址
				char rtspUrl[128] = { 0 };
				ret = ONVIF_MEDIA_GetStreamUri(2, &common, channel.sourceToken[ch],(enum ONVIF_MEDIA_STREAM_TYPE)s, rtspUrl);
				if (!ret)
				{
					printf("ch: %d  streamType: %d rtsp url: %s\n", ch+1, s, rtspUrl);
				}
			}	
		}	
	}

	system("pause");
	return 0;
}






