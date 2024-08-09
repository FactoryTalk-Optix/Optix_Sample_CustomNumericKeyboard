#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.HMIProject;
using FTOptix.Retentivity;
using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.CoreBase;
using FTOptix.Core;
using FTOptix.NetLogic;
using FTOptix.DataLogger;
using FTOptix.SQLiteStore;
using FTOptix.Store;
#endregion

public class AppendKeyboard : BaseNetLogic
{
    private IUAVariable typingDecimals;
    private int numberOfDecimals;
    private SpinBox sBox;

    public override void Start()
    {
        typingDecimals = LogicObject.GetVariable("typingDecimals");
        numberOfDecimals = 0;
        sBox = ((SpinBox)Owner);
        Clear();
    }

    [ExportMethod]
    public void AppendValue(string value)
    {
        var isBackspace = value == string.Empty;
        var sboxValue = Math.Round(sBox.Value, numberOfDecimals).ToString();
        var sboxValueHasDecimals = sboxValue.Contains(',');
        var integerPart = sboxValue.Split(',')[0];
        var decimalPart = sboxValueHasDecimals ? sboxValue.Split(',')[1].Substring(0, numberOfDecimals) : string.Empty;
        double res;

        if (sboxValueHasDecimals)
        {
            if (!isBackspace) numberOfDecimals++;
            decimalPart += value;
            res = RoundFloat(integerPart + "," + decimalPart);
        }
        else if ((bool)typingDecimals.Value.Value)
        {
            if (!isBackspace) numberOfDecimals++;
            res = RoundFloat(integerPart + "," + value);
            typingDecimals.Value = false;
        }
        else
        {
            if (!isBackspace)
            {
                res = int.Parse(sboxValue + value);
            } else
            {
                res = sboxValue.Length == 1 ? 0 : int.Parse(sboxValue.Substring(0, sboxValue.Length - 1));
            }
        }

        sBox.Value = res;
    }

    [ExportMethod]
    public void Clear()
    {
        sBox.Value = 0;
        numberOfDecimals = 0;
    }

    [ExportMethod]
    public void ToggleSign()
    {
        sBox.Value *= -1;
    }

    [ExportMethod]
    public void Backspace()
    {
        var sboxValueHasDecimals = sBox.Value.ToString().Contains(',');
        if (sboxValueHasDecimals)
        {
            numberOfDecimals--;
        }

        AppendValue(string.Empty);
    }

    private double RoundFloat(string s) => Math.Round(float.Parse(s), numberOfDecimals);
}
    
