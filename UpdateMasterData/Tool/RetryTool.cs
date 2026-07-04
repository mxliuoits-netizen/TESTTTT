using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PIC.Tools;

/// <summary>
/// 重複執行工具
/// </summary>
public class RetryTool
{
    public int RetryCount { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 1;
    public bool UseExponentialBackoff { get; set; } = false;
    public Action<string> LogFunc { get; set; } = (message) => { Console.WriteLine(message); };

    /// <summary>
    /// 重複執行
    /// </summary>
    /// <param name="action"></param>
    public void Execute(Action action)
    {
        int i = 0;
        while (true)
        {
            try
            {
                action.Invoke();
                return;
            }
            catch (Exception ex)
            {
                i++;
                if (i > RetryCount) throw;
                if (LogFunc != null) LogFunc.Invoke($"重新嘗試 第{i}次, 發生異常:" + ex.Message);
                int delay = RetryDelaySeconds;
                if (UseExponentialBackoff)
                    delay = RetryDelaySeconds * (int)Math.Pow(2, i - 1);
                Thread.Sleep(delay * 1000);
            }
        }
    }

}
