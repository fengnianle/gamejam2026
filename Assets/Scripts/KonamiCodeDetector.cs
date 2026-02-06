using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 经典的 Konami Code 彩蛋检测器
/// 输入 "上上下下左右左右BABA" 触发
/// </summary>
public class KonamiCodeDetector : MonoBehaviour
{
    [Header("目标控制器")]
    [Tooltip("需要控制的PlayerController")]
    public PlayerController playerController;

    [Header("作弊码序列")]
    private readonly KeyCode[] cheatCode = new KeyCode[] {
        KeyCode.UpArrow,
        KeyCode.UpArrow,
        KeyCode.DownArrow,
        KeyCode.DownArrow,
        KeyCode.LeftArrow,
        KeyCode.RightArrow,
        KeyCode.LeftArrow,
        KeyCode.RightArrow,
        KeyCode.B,
        KeyCode.A,
        KeyCode.B,
        KeyCode.A
    };

    [Header("关闭作弊码序列")]
    private readonly KeyCode[] disableCode = new KeyCode[] {
        KeyCode.X,
        KeyCode.Y,
        KeyCode.B
    };

    private List<KeyCode> inputHistory = new List<KeyCode>();
    private int cheatCodeIndex = 0;
    private int disableCodeIndex = 0;

    void Update()
    {
        if (playerController == null) return;

        if (Input.anyKeyDown)
        {
            // 检测是否按下了我们需要关注的键
            if (Input.GetKeyDown(KeyCode.UpArrow)) CheckInput(KeyCode.UpArrow);
            else if (Input.GetKeyDown(KeyCode.DownArrow)) CheckInput(KeyCode.DownArrow);
            else if (Input.GetKeyDown(KeyCode.LeftArrow)) CheckInput(KeyCode.LeftArrow);
            else if (Input.GetKeyDown(KeyCode.RightArrow)) CheckInput(KeyCode.RightArrow);
            else if (Input.GetKeyDown(KeyCode.A)) CheckInput(KeyCode.A);
            else if (Input.GetKeyDown(KeyCode.B)) CheckInput(KeyCode.B);
            else if (Input.GetKeyDown(KeyCode.X)) CheckInput(KeyCode.X);
            else if (Input.GetKeyDown(KeyCode.Y)) CheckInput(KeyCode.Y);
        }
    }

    private void CheckInput(KeyCode key)
    {
        // 1. 检测开启作弊码
        if (key == cheatCode[cheatCodeIndex])
        {
            cheatCodeIndex++;
            if (cheatCodeIndex == cheatCode.Length)
            {
                EnableCheat();
                cheatCodeIndex = 0; // 重置
            }
        }
        else
        {
            // 如果按错，看是否是第一个键，如果是则保留，否则重置
            cheatCodeIndex = (key == cheatCode[0]) ? 1 : 0;
        }

        // 2. 检测关闭作弊码
        if (key == disableCode[disableCodeIndex])
        {
            disableCodeIndex++;
            if (disableCodeIndex == disableCode.Length)
            {
                DisableCheat();
                disableCodeIndex = 0; // 重置
            }
        }
        else
        {
            disableCodeIndex = (key == disableCode[0]) ? 1 : 0;
        }
    }

    private void EnableCheat()
    {
        if (!playerController.autoCounterEnabled)
        {
            playerController.autoCounterEnabled = true;
            GameLogger.LogCheat("★ KONAMI CODE ACTIVATED! Auto Counter Enabled. ★");
            if (AudioManager.Instance != null)
            {
                // 播放胜利音效作为提示
                AudioManager.Instance.PlayKonamiCodeSound();
            }
        }
    }

    private void DisableCheat()
    {
        if (playerController.autoCounterEnabled)
        {
            playerController.autoCounterEnabled = false;
            GameLogger.LogCheat("Cheat Deactivated.");
        }
    }
}
