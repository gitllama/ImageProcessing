# PrismAutofacVTK

- Activiz.NET.x86 v 5.8.0
- Extended.Wpf.Toolkit v3.2.0
- System.ValueTuple v 4.4.0
- ReactiveProperty v 3.6.0

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

### foundation

#### 処理の流れ

1. Source : データの作成
1. (Filter : データの加工) 
1. Mapper : 表示可能なオブジェクトに写像
1. Actor : オブジェクトの調整（色、透明度）
1. Render : 描画

#### Source

```Python
cube=vtk.vtkCubeSource() # 立方体のSourceを生成
cone.SetResolution(40) # 解像度の設定
```

#### Mapper

```Python
mapper=vtk.vtkPolyDataMapper() # Mapperの生成
mapper.SetInput(cube.GetOutput()) # データを取得
```

#### Actor

```Python
actor=vtk.vtkActor() # Actorの生成
actor.SetMapper(mapper) # Mapperを追加
actor.GetProperty().SetOpacity(0.3) # 透明度を設定
actor.GetProperty().SetColor(1,0,0) # 色を設定
```

#### Render

```Python
renderer=vtk.vtkRenderer() # Rendererの生成
renderer.AddActor(actor) # Actorの追加
renderer.SetBackground( 0.1, 0.4, 0.2 ) # 背景色を設定

window=vtk.vtkRenderWindow() # Windowの生成
window.AddRenderer(renderer) # Rendererの追加
window.SetSize(600,600) # Windowのサイズ変更

renderer.GetActiveCamera().Zoom(0.8)     # ズーム
renderer.GetActiveCamera().Azimuth(30)   # y軸回転（鉛直）
renderer.GetActiveCamera().Elevation(45) # x軸回転
renderer.GetActiveCamera().Roll(0)       # z軸回転

window.Render() # 再描画
```

