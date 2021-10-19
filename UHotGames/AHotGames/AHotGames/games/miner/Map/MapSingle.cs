﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.UI;
using UnityEngine;

public class MapSingle : AHotBase
{
	protected override void InitComponents()
	{
		new UMinerMap().SetGameObj(gameObj);
		LoadUI<UIMinerBattle>();

		AClientApis.OnEnterScene();
	}
}
