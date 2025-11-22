using System.ComponentModel.DataAnnotations;
using YukkuriMovieMaker.Commons;
using YukkuriMovieMaker.Controls;
using YukkuriMovieMaker.Exo;
using YukkuriMovieMaker.ItemEditor.CustomVisibilityAttributes;
using YukkuriMovieMaker.Player.Video;
using YukkuriMovieMaker.Plugin.Shape;
using YukkuriMovieMaker.Project;

namespace MineSkinShape
{
    internal class MineSkinShapeParameter(SharedDataStore? sharedData) : ShapeParameterBase(sharedData)
    {
        [Display(Name = "サイズ")]
        [AnimationSlider("F0", "px", 0, 100)]
        public Animation Size { get; } = new Animation(100, 0, 1600);

        [Display(Name = "ファイル")]
        [FileSelector(YukkuriMovieMaker.Settings.FileGroupType.ImageItem)]
        public string SkinFile { get => skinFile; set => Set(ref skinFile, value); }
        string skinFile = "";

        [Display(GroupName = "描画", Name = "X軸回転")]
        [AnimationSlider("F0", "°", -360, 360)]
        public Animation RotateX { get; } = new Animation(0, -100000, 100000);
        [Display(GroupName = "描画", Name = "Y軸回転")]
        [AnimationSlider("F0", "°", -360, 360)]
        public Animation RotateY { get; } = new Animation(0, -100000, 100000);
        [Display(GroupName = "描画", Name = "Z軸回転")]
        [AnimationSlider("F0", "°", -360, 360)]
        public Animation RotateZ { get; } = new Animation(0, -100000, 100000);
        [Display(GroupName = "描画", Name = "詳細設定")]
        [ToggleSlider]
        public bool IsDetail { get => isDetail; set => Set(ref isDetail, value); }
        bool isDetail = false;

        [Display(GroupName = "頭", Name = "X軸回転")]
        [ShowPropertyEditorWhen(nameof(IsDetail),true)]
        [AnimationSlider("F0", "°", -360, 360)]
        public Animation Head_RotateX { get; } = new Animation(0, -100000, 100000);
        [Display(GroupName = "頭", Name = "Y軸回転")]
        [ShowPropertyEditorWhen(nameof(IsDetail), true)]
        [AnimationSlider("F0", "°", -360, 360)]
        public Animation Head_RotateY { get; } = new Animation(0, -100000, 100000);
        [Display(GroupName = "頭", Name = "Z軸回転")]
        [ShowPropertyEditorWhen(nameof(IsDetail), true)]
        [AnimationSlider("F0", "°", -360, 360)]
        public Animation Head_RotateZ { get; } = new Animation(0, -100000, 100000);
        [Display(GroupName = "右腕", Name = "X軸回転")]
        [ShowPropertyEditorWhen(nameof(IsDetail), true)]
        [AnimationSlider("F0", "°", -360, 360)]
        public Animation RightArm_RotateX { get; } = new Animation(0, -100000, 100000);
        [Display(GroupName = "右腕", Name = "Y軸回転")]
        [ShowPropertyEditorWhen(nameof(IsDetail), true)]
        [AnimationSlider("F0", "°", -360, 360)]
        public Animation RightArm_RotateY { get; } = new Animation(0, -100000, 100000);
        [Display(GroupName = "右腕", Name = "Z軸回転")]
        [ShowPropertyEditorWhen(nameof(IsDetail), true)]
        [AnimationSlider("F0", "°", -360, 360)]
        public Animation RightArm_RotateZ { get; } = new Animation(0, -100000, 100000);
        [Display(GroupName = "左腕", Name = "X軸回転")]
        [ShowPropertyEditorWhen(nameof(IsDetail), true)]
        [AnimationSlider("F0", "°", -360, 360)]
        public Animation LeftArm_RotateX { get; } = new Animation(0, -100000, 100000);
        [Display(GroupName = "左腕", Name = "Y軸回転")]
        [ShowPropertyEditorWhen(nameof(IsDetail), true)]
        [AnimationSlider("F0", "°", -360, 360)]
        public Animation LeftArm_RotateY { get; } = new Animation(0, -100000, 100000);
        [Display(GroupName = "左腕", Name = "Z軸回転")]
        [ShowPropertyEditorWhen(nameof(IsDetail), true)]
        [AnimationSlider("F0", "°", -360, 360)]
        public Animation LeftArm_RotateZ { get; } = new Animation(0, -100000, 100000);
        [Display(GroupName = "右脚", Name = "X軸回転")]
        [ShowPropertyEditorWhen(nameof(IsDetail), true)]
        [AnimationSlider("F0", "°", -360, 360)]
        public Animation RightLeg_RotateX { get; } = new Animation(0, -100000, 100000);
        [Display(GroupName = "右脚", Name = "Y軸回転")]
        [ShowPropertyEditorWhen(nameof(IsDetail), true)]
        [AnimationSlider("F0", "°", -360, 360)]
        public Animation RightLeg_RotateY { get; } = new Animation(0, -100000, 100000);
        [Display(GroupName = "右脚", Name = "Z軸回転")]
        [ShowPropertyEditorWhen(nameof(IsDetail), true)]
        [AnimationSlider("F0", "°", -360, 360)]
        public Animation RightLeg_RotateZ { get; } = new Animation(0, -100000, 100000);
        [Display(GroupName = "左脚", Name = "X軸回転")]
        [ShowPropertyEditorWhen(nameof(IsDetail), true)]
        [AnimationSlider("F0", "°", -360, 360)]
        public Animation LeftLeg_RotateX { get; } = new Animation(0, -100000, 100000);
        [Display(GroupName = "左脚", Name = "Y軸回転")]
        [ShowPropertyEditorWhen(nameof(IsDetail), true)]
        [AnimationSlider("F0", "°", -360, 360)]
        public Animation LeftLeg_RotateY { get; } = new Animation(0, -100000, 100000);
        [Display(GroupName = "左脚", Name = "Z軸回転")]
        [ShowPropertyEditorWhen(nameof(IsDetail), true)]
        [AnimationSlider("F0", "°", -360, 360)]
        public Animation LeftLeg_RotateZ { get; } = new Animation(0, -100000, 100000);

        public MineSkinShapeParameter() : this(null) { }

        public override IEnumerable<string> CreateMaskExoFilter(int keyFrameIndex, ExoOutputDescription desc, ShapeMaskExoOutputDescription shapeMaskParameters)
        {
            return [];
        }

        public override IEnumerable<string> CreateShapeItemExoFilter(int keyFrameIndex, ExoOutputDescription desc)
        {
            return [];
        }

        public override IShapeSource CreateShapeSource(IGraphicsDevicesAndContext devices)
        {
            return new MineSkinShapeSource(devices, this);
        }

        protected override IEnumerable<IAnimatable> GetAnimatables() => [
            Size,
            RotateX, RotateY, RotateZ,
            Head_RotateX,Head_RotateY,Head_RotateZ,
            RightArm_RotateX, RightArm_RotateY,RightArm_RotateZ,
            LeftArm_RotateX,LeftArm_RotateY,LeftArm_RotateZ,
            RightLeg_RotateX,RightLeg_RotateY,RightLeg_RotateZ,
            LeftLeg_RotateX,LeftLeg_RotateY,LeftLeg_RotateZ
            ];

        protected override void LoadSharedData(SharedDataStore store)
        {
            var sharedData = store.Load<SharedData>();
            if (sharedData is null)
                return;

            sharedData.CopyTo(this);
        }

        protected override void SaveSharedData(SharedDataStore store)
        {
            store.Save(new SharedData(this));
        }

        class SharedData
        {
            public Animation Size { get; } = new Animation(100, 0, 1000);
            public SharedData(MineSkinShapeParameter param)
            {
                Size.CopyFrom(param.Size);
            }
            public void CopyTo(MineSkinShapeParameter param)
            {
                param.Size.CopyFrom(Size);
            }
        }
    }
}
