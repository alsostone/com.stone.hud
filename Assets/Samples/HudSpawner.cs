using UnityEngine;
using Random = UnityEngine.Random;
using ST.HUD;
using UnityEngine.UI;

public class HudSpawner : MonoBehaviour
{
    public GameObject capsule;
    public Text textCount;

    void Start()
    {
        // 测试生成1000个玩家名字耗时
        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();
        {
            for (int i = 0; i < 1000; ++i)
            {
                HudRenderer.Instance.TextDraw("玩家名称七个字" + i);
            }
        }
        stopwatch.Stop();
        Debug.LogFormat("生成1000个玩家名字耗时:{0}ms", stopwatch.ElapsedMilliseconds);
        CreatePlayer();
    }
    
    void CreatePlayer()
    {
        for (var i = 0; i < 1000; i++)
        {
            var position = new Vector3(Random.Range(-100f, 100f), 0, Random.Range(-100f, 100f));
            HudRenderer.Instance.AddInstance("玩家名称七个字" + i, position + Vector3.up * 1.5f, Random.Range(0f, 1f));
            
            var cube = Instantiate(capsule);
            cube.transform.position = position;
        }
        textCount.text = HudRenderer.Instance.GetCount().ToString();
    }

    public void OnAddButton1000()
    {
        for (var i = 0; i < 1000; i++)
        {
            var position = new Vector3(Random.Range(-100f, 100f), 0, Random.Range(-100f, 100f));
            HudRenderer.Instance.AddInstance("玩家名称七个字" + i, position + Vector3.up * 1.5f, Random.Range(0f, 1f));
        }
        textCount.text = HudRenderer.Instance.GetCount().ToString();
    }
    
    public void OnAddButton10000()
    {
        for (var i = 0; i < 10000; i++)
        {
            // 名字生成个数支持1023个 若超过可以自行把Texture2DArray拓展成多个
            var position = new Vector3(Random.Range(-100f, 100f), 0, Random.Range(-100f, 100f));
            HudRenderer.Instance.AddInstance("玩家名称七个字" + (i % 1023), position + Vector3.up * 1.5f, Random.Range(0f, 1f));
        }
        textCount.text = HudRenderer.Instance.GetCount().ToString();
    }
    
    public void OnSubButton1000()
    {
        // 测试直接删除后边1000个
        // 正式项目中通过HudRenderer.Instance.AddInstance的返回值来删除
        var count = HudRenderer.Instance.GetCount();
        for (var i = count - 1000; i < count; i++)
        {
            HudRenderer.Instance.RemoveInstance(i);
        }
        textCount.text = HudRenderer.Instance.GetCount().ToString();
    }
}
