﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.UI;
using UnityEngine;

public class UICommonWait : AHotBase
{
	protected override bool bCanBeAutoClosed => false;
	private static UICommonWait sinstance;
	protected override void InitComponents()
	{
		sinstance = this;

		var circle = FindWidget<Transform>("circle");

		addUpdateAction(() =>
		{
			var euler = circle.localEulerAngles;
			euler.z -= Time.deltaTime * 100;
			circle.localEulerAngles = euler;
			return false;
		});
	}
	public static void Show()
	{
		if (sinstance == null)
			sinstance = LoadClass<UICommonWait>("UI/UICommonWait", null);
		else
			sinstance.gameObj?.SetActive(true);
	}
	public static void Hide()
	{
		if (sinstance != null)
			sinstance.gameObj?.SetActive(false);
	}
}

