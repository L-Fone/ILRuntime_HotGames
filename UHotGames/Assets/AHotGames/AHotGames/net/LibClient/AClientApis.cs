﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibClient;
using LibPacket;
using LibCommon;
using LibClient.GameObj;

public static class AClientApis
{
	public static void OnLogin(string username, string password, EPartnerID ePartnerID)
	{
		var req = new PktLoginRequest();
		req.username = username;
		req.password = password;
		req.ePartnerID = (int)ePartnerID;
		AClientApp.RemoteCall<PktLoginResult>(req, OnLoginCb);
	}

	private static void OnLoginCb(PktLoginResult res)
	{
		AClientApp.clientComm.rcvLoginCb(res.bSuccess, res.unionid, (EPartnerID)res.ePartnerID);
	}

	internal static void OnEnterGame()
	{
		var req = new PktEnterGameRequest();
		AClientApp.RemoteCall<PktEnterGameResult>(req, OnEnterGameCb);
	}

	internal static void OnEnterScene()
	{
		var pkt = new PktCommonRequest();
		pkt.method = (int)ECommonMethod.EnterScene;
		AClientApp.RemoteCall(pkt);
	}

	internal static void OnEnterCity()
	{
		var pkt = new PktCommonRequest();
		pkt.pData.intArg = 1;
		pkt.method = (int)ECommonMethod.EnterConta;
		AClientApp.RemoteCall(pkt);
	}

	internal static void OnBeginFight()
	{
		var pkt = new PktCommonRequest();
		pkt.method = (int)ECommonMethod.BeginFight;
		AClientApp.RemoteCall(pkt);
	}

	internal static void OnEnterConta(string swx)
	{
		var pkt = new PktCommonRequest();
		pkt.pData.strArg = swx;
		pkt.method = (int)ECommonMethod.EnterConta;
		AClientApp.RemoteCall(pkt);
	}

	private static void OnEnterGameCb(PktEnterGameResult res)
	{
		if (res.info != null)
		{
			CakeClient.AddCake(new CakeAvatar(res.info.avatarID, res.info));
			CakeAvatar.myID = res.info.avatarID;
		}
		AClientApp.clientComm.rcvEnterGameCb(res.info);
	}

	public static void OnCreateAvatar(string avatarName, int sex)
	{
		var req = new PktCreateAvatarRequest();
		req.avatarName = avatarName;
		req.sex = sex;
		AClientApp.RemoteCall<PktCreateAvatarResult>(req, OnCreateAvatarCb);
	}

	private static void OnCreateAvatarCb(PktCreateAvatarResult res)
	{
		if (res.info != null)
		{
			CakeClient.AddCake(new CakeAvatar(res.info.avatarID, res.info));
			CakeAvatar.myID = res.info.avatarID;
		}
		AClientApp.clientComm.rcvCreateAvatarCb(res.eResult, res.info);
	}

	public static void OnGetSdata(string name = "")
	{
		var pkt = new PktGetSdata();
		pkt.name = name;
		AClientApp.RemoteCall(pkt);
	}

	public static void OnDailyCheck()
	{
		var pkt = new PktDailyCheckRequest();
		AClientApp.RemoteCall<PktDailyCheckResult>(pkt, OnDailyCheckCb);
	}
	private static void OnDailyCheckCb(PktDailyCheckResult obj)
	{
		AClientApp.clientComm.rcvDailyCheckCb(obj.eResult, obj.lItems);
	}
	public static void OnPay(int productID)
	{
		PktPayRequest pkt = new PktPayRequest();
		pkt.productID = productID;
		AClientApp.RemoteCall(pkt);
	}
	public static void OnExchange(int count)
	{
		PktExchangeRequest pkt = new PktExchangeRequest();
		pkt.count = count;
		pkt.eType = PktExchangeRequest.EType.Gold;
		AClientApp.RemoteCall<PktExchangeResult>(pkt, ExchangeCb);
	}
	private static void ExchangeCb(PktExchangeResult result)
	{
		AClientApp.clientComm.rcvExchangeCb(result.bSuccess);
	}

	public static void OnCreateOrder(int productID)
	{
		var pkt = new PktCreateOrderRequest();
		pkt.productID = productID;
		AClientApp.RemoteCall<PktCreateOrderResult>(pkt, CreateOrderCb);
	}

	private static void CreateOrderCb(PktCreateOrderResult obj)
	{
		AClientApp.clientComm.rcvCreateOrderCb(obj.eResult, obj.orderID, obj.extraInfo);
	}

}
