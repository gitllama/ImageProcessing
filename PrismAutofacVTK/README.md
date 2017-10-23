# PrismAutofacVTK

Activiz.NET.x86 v 5.8.0
Extended.Wpf.Toolkit v3.2.0
System.ValueTuple v 4.4.0
ReactiveProperty v 3.6.0

## VTK (Visualization Toolkit)

https://github.com/Kitware/VTK  
https://lorensen.github.io/VTKExamples/site/

BSD 3-clause Licenseで使用できる3Dコンピュータグラフィックス・画像処理・可視化ライブラリ  
幅広い言語サポートと、医用画像処理等に活用されている（ので汎用的に使用できそう）

### Setting

1.ツール > NuGetパッケージマネージャー > パッケージの管理

ActiViz.NET x86 参照追加

2.プロジェクト > 参照の追加

System.Windoes.Forms
WindowsFormsIntegration

3.Prism.Events + Messenger実装

コードビハインドの場合

```C#
public MainWindow()
{
  InitializeComponent();
  RenderControl.Load += RenderWindowControlOnLoad;
}

void RenderWindowControlOnLoad(object sender, EventArgs e)
{
  using (var reader = new Kitware.VTK.vtkPLYReader())
  using (var mapper = new Kitware.VTK.vtkCompositePolyDataMapper())
  using (var actor = new Kitware.VTK.vtkActor())
  using (var renderer = RenderControl.RenderWindow.GetRenderers().GetFirstRenderer())
  {
    reader.SetFileName(@"bun_zipper.ply");
    mapper.SetInputConnection(reader.GetOutputPort());
    actor.SetMapper(mapper);
		renderer.AddActor(actor);
    RenderControl.RenderWindow.Render();
  }
}
```

バインドする際は

WindowsFormsHostだとそもそもBindingできないので
BehaviorはあきらめてPrism.Events + Messengerで対応

```C#
public class Messenger : EventAggregator
{
  private static Messenger _instance;
  public static Messenger Instance { get => _instance ?? (_instance = new Messenger()); }
}
public MainWindow()
{
　InitializeComponent();
  Messenger.Instance.GetEvent<PubSubEvent<vtkActor>>().Subscribe(m =>{
    using (var renderer = RenderControl.RenderWindow.GetRenderers().GetFirstRenderer())
    {
      renderer.AddActor(actor);
      RenderControl.RenderWindow.Render();
    }
  };
　//VM : Messenger.Instance.GetEvent<PubSubEvent<vtkActor>>().Publish(actor);
}
```
