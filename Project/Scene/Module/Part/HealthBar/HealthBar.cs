using Godot;
using Moinros.CSharp.Util;
using System;

namespace GodotModule.Project.Scene.Module.Part.HealthBar
{

    /// <summary>
    /// 血条组件：支持多条血条、血量增加时极速填充动画、血量减少时过渡色块动画、颜色轮换规则、血量显示
    /// </summary>
    public partial class HealthBar : Control
    {

        /// <summary>
        /// 血条颜色结构
        /// </summary>
        public struct HealthBarColor
        {
            public Color Color;
            public string Name;
        }

        /// <summary>
        /// 默认血条颜色配置
        /// </summary>
        private readonly HealthBarColor[] DefaultColors =
        [
            new HealthBarColor { Name = "purple", Color = Color.Color8(62, 79, 195) },
            new HealthBarColor { Name = "blue", Color = Color.Color8(39, 117, 240) },
            new HealthBarColor { Name = "green", Color = Color.Color8(0, 181, 0) },
            new HealthBarColor { Name = "yellow", Color = Color.Color8(240, 110, 39) },
            new HealthBarColor { Name = "red", Color = Color.Color8(255, 0, 0) }
        ];

        /// <summary>
        /// 当前血条颜色配置
        /// </summary>
        private HealthBarColor[] CurrentColors;

        /// <summary>
        /// 最大血量
        /// </summary>
        public int HealthMax { get; private set; }
        /// <summary>
        /// 当前血量
        /// </summary>
        public int HealthValue { get; private set; }

        /// <summary>
        /// 单根血条最大值
        /// </summary>
        public int HealthBarSingleValueMax { get; private set; }

        /// <summary>
        /// 血条总数
        /// </summary>
        public int HealthBarTotalCount { get; private set; }

        /// <summary>
        /// 当前血条计数
        /// </summary>
        public int HealthBarCurrentCount { get; private set; }

        /// <summary>
        /// 最上层血条
        /// </summary>
        [Export]
        ProgressBar HealthBarUpper;

        /// <summary>
        /// 下一根血条的颜色
        /// </summary>
        [Export]
        ColorRect HealthBarUnderColor;

        /// <summary>
        /// 显示血条计数标签
        /// </summary>
        [Export]
        Label HealthBarCurrentCountLabel;

        /// <summary>
        /// 显示血条计数
        /// </summary>
        void ShowHealthBarCount(int value)
        {
            HealthBarCurrentCountLabel.Text = $"x{value}";
        }

        /// <summary>
        /// 显示当前血量标签
        /// </summary>
        [Export]
        Label HealthValueLabel;

        /// <summary>
        /// 显示血量最大值标签
        /// </summary>
        [Export]
        Label HealthMaxLabel;

        /// <summary>
        /// 上层血条填充样式
        /// </summary>
        private readonly StyleBoxFlat _upperFillStyle = new();

        /// <summary>
        /// 过渡动画时长（秒）：受击后失去血量区间的色块由亮变深直至消失的时间
        /// </summary>
        [Export]
        float TransitionDuration = 0.5f;

        /// <summary>
        /// 过渡动画起始颜色（亮白闪烁）
        /// </summary>
        [Export]
        Color TransitionColorBright = new(1f, 1f, 1f);

        /// <summary>
        /// 过渡动画结束颜色（暗色）
        /// </summary>
        [Export]
        Color TransitionColorDark = new(0.25f, 0.25f, 0.25f);

        /// <summary>
        /// 血量增加时的填充动画时长（秒）：血条以极快速度填充到新血量值
        /// </summary>
        [Export]
        float HealDuration = 0.2f;

        /// <summary>
        /// 一个正在播放的过渡色块（受击后失去血量区间的颜色衰减动画）
        /// </summary>
        private sealed class TransitionChunk
        {
            /// <summary>色块节点（最上层血条的子节点，绘制在血条填充之下，只露出失去的血量部分）</summary>
            public ColorRect Rect;
            /// <summary>已播放时间（秒）</summary>
            public float Elapsed;
            /// <summary>总时长（秒）</summary>
            public float Duration;
            /// <summary>起始颜色（亮色闪烁）</summary>
            public Color StartColor;
            /// <summary>结束颜色（变深且完全透明，即消失）</summary>
            public Color EndColor;
        }

        /// <summary>
        /// 正在播放的过渡色块（短时间内多次受击时各自独立共存）
        /// </summary>
        private readonly LinkList<TransitionChunk> _activeChunks = new();


        public override void _Ready()
        {
            // 初始化颜色配置
            CurrentColors = new HealthBarColor[DefaultColors.Length];
            Array.Copy(DefaultColors, CurrentColors, DefaultColors.Length);

            // 使用纯色 StyleBox 替代贴图：填充颜色在代码中动态修改
            HealthBarUpper.AddThemeStyleboxOverride("background", new StyleBoxEmpty());
            HealthBarUpper.AddThemeStyleboxOverride("fill", _upperFillStyle);

            // 纯色血条不显示百分比文字
            HealthBarUpper.ShowPercentage = false;

            SetHealthBarSingleValueMax(1000);
            UpdateHealthBarColors();
        }


        public override void _Process(double delta)
        {
            base._Process(delta);
            // 推进血量填充动画（血量增加时血条以极快速度填充到新值）
            UpdateHealAnimation((float)delta);
            // 推进所有过渡色块的颜色衰减（各色块独立计时，互不影响，可共存）
            UpdateTransitionChunks((float)delta);
        }
        /// <summary>
        /// 设置血条颜色配置
        /// </summary>
        public void SetHealthBarColors(HealthBarColor[] colors)
        {
            if (colors == null || colors.Length == 0)
            {
                CurrentColors = DefaultColors;
            }
            else
            {
                CurrentColors = colors;
            }
            UpdateHealthBarColors();
        }

        /// <summary>
        /// 更新血条颜色显示（上层条填充色、下层条下一根颜色）
        /// </summary>
        private void UpdateHealthBarColors()
        {
            UpdateHealthBarColors(HealthBarCurrentCount);
        }

        /// <summary>
        /// 更新血条颜色显示（上层条填充色、下层条下一根颜色），按指定血条计数计算下层条颜色
        /// </summary>
        private void UpdateHealthBarColors(int barCount)
        {
            // 更新上层血条填充颜色
            _upperFillStyle.BgColor = CurrentColors[Mathf.Clamp(_colorIndex, 0, CurrentColors.Length - 1)].Color;

            // 更新下层血条颜色
            if (barCount > 1)
            {
                HealthBarUnderColor.Color = CurrentColors[GetColorIndexFromBarCount(barCount - 1)].Color;
            }
            else
            {
                HealthBarUnderColor.Color = Colors.Transparent;
            }
        }
        /// <summary>
        /// 获取当前血条颜色配置
        /// </summary>
        public HealthBarColor[] GetHealthBarColors()
        {
            var colors = new HealthBarColor[CurrentColors.Length];
            Array.Copy(CurrentColors, colors, CurrentColors.Length);
            return colors;
        }

        /// <summary>
        /// 获取指定索引的血条颜色
        /// </summary>
        public HealthBarColor GetHealthBarColor(int index)
        {
            if (index < 0 || index >= CurrentColors.Length)
            {
                return DefaultColors[0];
            }
            return CurrentColors[index];
        }

        /// <summary>
        /// 设置指定索引的血条颜色
        /// </summary>
        public void SetHealthBarColor(int index, Color color)
        {
            if (index >= 0 && index < CurrentColors.Length)
            {
                CurrentColors[index].Color = color;
                UpdateHealthBarColors();
            }
        }

        /// <summary>
        /// 更新最上层血条的进度值与过渡动画
        /// 血量增加时血条以极快速度动画填充到新值；扣血时直接定位并生成过渡色块
        /// </summary>
        private void UpdateHealthBarValue()
        {
            if (HealthBarSingleValueMax <= 0) return;

            // 更新前上层血条显示的值（作为条内扣血时过渡色块的右端）
            int previousValueInBar = (int)HealthBarUpper.Value;

            if (HealthValue > _lastHealthValue)
            {
                // 血量增加：清除仍在播放的过渡色块，血条以极快速度动画填充到新值（不直接跳变）
                ClearTransitionChunks();
                StartHealAnimation();
            }
            else if (HealthValue < _lastHealthValue)
            {
                // 扣血：中断填充动画并直接定位到当前血量
                CancelHealAnimation();
                int valueInBar = (int)GetBarValueInBar(HealthValue);
                HealthBarUpper.Value = valueInBar;

                // 在失去的血量区间上生成过渡色块（纯颜色由亮变深直至消失，不使用进度条）
                // 掉整条（或多条）时色块延伸到满条位置；条内扣血时延伸到更新前的值
                int lostFrom = valueInBar;
                int lostTo = HealthBarCurrentCount < _lastBarCount ? HealthBarSingleValueMax : previousValueInBar;
                if (lostTo > lostFrom)
                {
                    SpawnTransitionChunk(lostFrom, lostTo);
                }

                // 根据剩余血条数切换颜色（非最后一条血在前 N-1 个颜色中循环，最后一条血必定为末位颜色红色）
                SetColorIndex(GetColorIndexFromBarCount(HealthBarCurrentCount));
            }
            else
            {
                // 血量不变：无填充动画时直接定位；有填充动画时进度与颜色均由动画继续驱动
                if (!_healAnimating)
                {
                    HealthBarUpper.Value = GetBarValueInBar(HealthValue);
                    SetColorIndex(GetColorIndexFromBarCount(HealthBarCurrentCount));
                }
            }

            _lastBarCount = HealthBarCurrentCount;
            _lastHealthValue = HealthValue;
        }


        /// <summary>
        /// 在 [fromValue, toValue] 血量区间上生成一个过渡色块
        /// 色块作为最上层血条的子节点并绘制在其填充之下：
        /// 只露出当前填充右侧“失去的血量”部分，血量恢复后会被填充自然遮挡
        /// 色块颜色由亮变深、逐渐透明，播放完毕后自动销毁
        /// </summary>
        private void SpawnTransitionChunk(int fromValue, int toValue)
        {
            ColorRect chunk = new()
            {
                Name = "TransitionChunk",
                // 绘制在父节点（上层血条的填充）之下
                ShowBehindParent = true,
                // 不拦截鼠标事件
                MouseFilter = MouseFilterEnum.Ignore,
                Color = TransitionColorBright,
            };
            // 用锚点按血量比例定位：色块自动跟随血条尺寸变化
            chunk.SetAnchorAndOffset(Side.Left, fromValue / (float)HealthBarSingleValueMax, 0);
            chunk.SetAnchorAndOffset(Side.Top, 0f, 0);
            chunk.SetAnchorAndOffset(Side.Right, toValue / (float)HealthBarSingleValueMax, 0);
            chunk.SetAnchorAndOffset(Side.Bottom, 1f, 0);
            HealthBarUpper.AddChild(chunk);

            _activeChunks.AddLast(new TransitionChunk
            {
                Rect = chunk,
                Elapsed = 0f,
                Duration = Mathf.Max(TransitionDuration, 0.01f),
                StartColor = TransitionColorBright,
                // 结束态：颜色变深且完全透明（消失）
                EndColor = new Color(TransitionColorDark.R, TransitionColorDark.G, TransitionColorDark.B, 0f),
            });
        }

        /// <summary>
        /// 逐帧推进所有过渡色块的颜色衰减，播放完毕后销毁色块
        /// </summary>
        private void UpdateTransitionChunks(float delta)
        {
            TransitionChunk chunk = _activeChunks.Next();
            while (chunk != null)
            {
                chunk.Elapsed += delta;
                float t = chunk.Elapsed / chunk.Duration;
                if (t < 1f)
                {
                    chunk.Rect.Color = chunk.StartColor.Lerp(chunk.EndColor, t);
                }
                else
                {
                    chunk.Rect.QueueFree();
                    _activeChunks.Remove(chunk);
                }
                chunk = _activeChunks.Next();
            }

        }

        /// <summary>
        /// 立即清除所有正在播放的过渡色块（加血时不保留扣血残影）
        /// </summary>
        private void ClearTransitionChunks()
        {
            // _activeChunks.RestoreCursor();
            TransitionChunk chunk = _activeChunks.Next();
            while (chunk != null)
            {
                chunk.Rect.QueueFree();
                _activeChunks.Remove(chunk);
                chunk = _activeChunks.Next();
            }
            // 节点也要全部移出链表，否则下一帧会遍历到已释放的 Godot 对象
            _activeChunks.RemoveAll();
        }

        /// <summary>
        /// 设置过渡动画时长（秒）；只影响之后生成的过渡色块，正在播放的不受影响
        /// </summary>
        public void SetTransitionDuration(float seconds)
        {
            TransitionDuration = Mathf.Max(seconds, 0.01f);
        }


        // ---------------- 血量填充动画（血量增加时血条极速填充到新值） ----------------

        /// <summary>
        /// 填充动画是否正在播放
        /// </summary>
        private bool _healAnimating = false;

        /// <summary>
        /// 填充动画已播放时间（秒）
        /// </summary>
        private float _healElapsed;

        /// <summary>
        /// 填充动画时长快照（秒，动画开始时取自 HealDuration，播放中不受参数修改影响）
        /// </summary>
        private float _healDuration;

        /// <summary>
        /// 填充动画起始血量
        /// </summary>
        private double _healFromValue;

        /// <summary>
        /// 填充动画目标血量
        /// </summary>
        private double _healToValue;

        /// <summary>
        /// 填充动画当前显示血量（连续增加时从当前位置续接）
        /// </summary>
        private double _healDisplayValue;

        /// <summary>
        /// 计算总血量在当前血条内的显示值（0 ~ HealthBarSingleValueMax；恰好整条满时显示满条）
        /// </summary>
        private double GetBarValueInBar(double totalValue)
        {
            if (totalValue <= 0d) { return 0d; }
            double valueInBar = totalValue % HealthBarSingleValueMax;
            if (valueInBar == 0d)
            {
                valueInBar = HealthBarSingleValueMax;
            }
            return valueInBar;
        }

        /// <summary>
        /// 计算总血量对应的剩余血条计数
        /// </summary>
        private int GetBarCountFromValue(double totalValue)
        {
            if (totalValue <= 0d) { return 0; }
            return (int)Math.Ceiling(totalValue / HealthBarSingleValueMax);
        }

        /// <summary>
        /// 开始填充动画：血条以极快速度从当前显示位置填充到新血量
        /// </summary>
        private void StartHealAnimation()
        {
            // 起点：已有填充动画进行中则从当前显示位置续接，否则从更新前的血量开始
            _healFromValue = _healAnimating ? _healDisplayValue : _lastHealthValue;
            _healToValue = HealthValue;
            _healElapsed = 0f;
            _healDuration = Mathf.Max(HealDuration, 0.01f);
            _healAnimating = true;
            // 立即应用起始显示状态（血条保持在当前位置，随后逐帧极速填充）
            _healDisplayValue = _healFromValue;
            ApplyHealDisplayValue(_healDisplayValue);
        }

        /// <summary>
        /// 逐帧推进填充动画：显示血量极速趋近目标血量，到达后结束
        /// </summary>
        private void UpdateHealAnimation(float delta)
        {
            if (!_healAnimating) { return; }

            _healElapsed += delta;
            float t = _healElapsed / _healDuration;
            if (t >= 1f)
            {
                // 到达目标血量：结束动画
                _healDisplayValue = _healToValue;
                _healAnimating = false;
            }
            else
            {
                _healDisplayValue = _healFromValue + (_healToValue - _healFromValue) * t;
            }
            ApplyHealDisplayValue(_healDisplayValue);
        }

        /// <summary>
        /// 立即中断填充动画（显示定位由调用方负责）
        /// </summary>
        private void CancelHealAnimation()
        {
            _healAnimating = false;
        }

        /// <summary>
        /// 按显示血量刷新血条：条内进度、计数标签与颜色（填充动画期间逐帧调用）
        /// 跨条增加时计数与颜色随动画逐条推进，动画结束时收敛到最终状态
        /// </summary>
        private void ApplyHealDisplayValue(double displayValue)
        {
            // 手动夹紧到 [0, HealthMax]，避免异常边界值导致异常
            double clamped = displayValue;
            if (clamped < 0d) { clamped = 0d; }
            else if (clamped > HealthMax) { clamped = HealthMax; }

            int barCount = GetBarCountFromValue(clamped);
            HealthBarUpper.Value = GetBarValueInBar(clamped);
            ShowHealthBarCount(barCount);
            // 上层条与下层条颜色均按当前显示血条数计算（动画结束时收敛到最终颜色）
            _colorIndex = GetColorIndexFromBarCount(barCount);
            UpdateHealthBarColors(barCount);
        }

        /// <summary>
        /// 设置血量增加时的填充动画时长（秒）；只影响之后的填充动画，正在播放的不受影响
        /// </summary>
        public void SetHealDuration(float seconds)
        {
            HealDuration = Mathf.Max(seconds, 0.01f);
        }


        /// <summary>
        /// 设置单条血条最大值
        /// </summary>
        public void SetHealthBarSingleValueMax(int value)
        {
            HealthBarSingleValueMax = value;
            HealthBarUpper.MaxValue = value;
            // 中断进行中的填充动画并直接定位到当前血量，避免条内值映射变化导致显示错位
            CancelHealAnimation();
            HealthBarUpper.Value = GetBarValueInBar(HealthValue);
        }


        /// <summary>
        /// 设置最大生命值
        /// 若最大值小于当前血量：同步夹紧当前血量，并刷新血条颜色、计数与进度显示
        /// </summary>
        public void SetHealthMax(int value)
        {
            HealthMax = value;
            HealthMaxLabel.Text = value.ToString();
            HealthBarTotalCount = (int)Math.Ceiling((double)HealthMax / HealthBarSingleValueMax);

            // 最大值小于当前血量：同步夹紧当前血量并刷新数值标签
            if (HealthValue > HealthMax)
            {
                HealthValue = HealthMax;
                HealthValueLabel.Text = HealthValue.ToString();

                // 重置增减判断基准并清除过渡色块与填充动画：最大值收缩属于配置刷新，不播放扣血过渡动画
                ClearTransitionChunks();
                CancelHealAnimation();
                _lastHealthValue = HealthValue;
                _lastBarCount = GetHealthBarCount();
            }

            HealthBarCurrentCount = GetHealthBarCount();
            ShowHealthBarCount(HealthBarCurrentCount);
            // 刷新血条进度与颜色显示
            UpdateHealthBarValue();
        }


        /// <summary>
        /// 统一入口：应用新的生命值（绝对值）并刷新标签与血条显示
        /// 自动夹紧到 [0, HealthMax]；扣血生成过渡色块，加血触发极速填充动画
        /// </summary>
        private void ApplyHealthValue(int value)
        {
            HealthValue = Mathf.Clamp(value, 0, HealthMax);
            HealthValueLabel.Text = HealthValue.ToString();
            HealthBarCurrentCount = GetHealthBarCount();
            ShowHealthBarCount(HealthBarCurrentCount);
            UpdateHealthBarValue();
        }

        /// <summary>
        /// 修改当前生命值（传入增量：正数加血、负数扣血）
        /// 扣血触发过渡色块动画，加血触发极速填充动画
        /// </summary>
        public void ModifyHealthValue(int value)
        {
            ApplyHealthValue(HealthValue + value);
        }

        /// <summary>
        /// 设置当前生命值（传入绝对值）
        /// 血条直接切换到对应的颜色：血量减少触发过渡色块动画，血量增加触发极速填充动画
        /// </summary>
        public void SetHealthValue(int value)
        {
            ApplyHealthValue(value);
        }

        /// <summary>
        /// 减少生命值（触发过渡动画）
        /// </summary>
        /// <param name="value"></param>
        public void HealthReduce(int value)
        {
            ModifyHealthValue(-value);
        }

        /// <summary>
        /// 增加生命值（血条以极快速度填充到新值）
        /// </summary>
        /// <param name="value"></param>
        public void HealthIncrease(int value)
        {
            ModifyHealthValue(value);
        }


        /// <summary>
        /// 获取剩余血条数量
        /// </summary>
        /// <returns></returns>
        public int GetHealthBarCount()
        {
            if (HealthValue <= 0) { return 0; }
            return (int)Math.Ceiling((double)HealthValue / HealthBarSingleValueMax);
        }


        /// <summary>
        /// 上次更新时的血条计数（用于判断是否掉整条）
        /// </summary>
        private int _lastBarCount;

        /// <summary>
        /// 上次更新时的血量值（用于判断血量增减方向）
        /// </summary>
        private int _lastHealthValue;


        /// <summary>
        /// 血条颜色索引
        /// </summary>
        private int _colorIndex = 0;

        /// <summary>
        /// 根据剩余血条计数计算颜色索引（颜色轮换规则）
        /// 非最后一条血在前 N-1 个颜色中循环轮换；仅剩最后一条血时必定使用末位颜色（默认红）
        /// </summary>
        int GetColorIndexFromBarCount(int barCount)
        {
            // 最后一条血（及空血条）必定使用末位颜色
            if (barCount <= 1)
            {
                return CurrentColors.Length - 1;
            }
            // 轮换色数量：前 N-1 个颜色（末位颜色保留给最后一条血）
            int cycleLength = CurrentColors.Length - 1;
            // 颜色配置不足 2 个时无从轮换，固定使用首色
            if (cycleLength <= 0)
            {
                return 0;
            }
            // 其余血条在前 N-1 个颜色中循环：满血为首色，每掉一整条推进一格
            return (HealthBarTotalCount - barCount) % cycleLength;
        }

        /// <summary>
        /// 下一个颜色的索引
        /// </summary>
        int NextColorIndex()
        {
            return (_colorIndex + 1) % CurrentColors.Length;
        }

        /// <summary>
        /// 上一个颜色的索引
        /// </summary>
        int PreviousColorIndex()
        {
            return (_colorIndex - 1 + CurrentColors.Length) % CurrentColors.Length;
        }


        /// <summary>
        /// 设置血条颜色索引
        /// </summary>
        public void SetColorIndex(int index)
        {
            _colorIndex = Mathf.Clamp(index, 0, CurrentColors.Length - 1);
            // 更新颜色显示（含下层血条显示的下一根颜色）
            UpdateHealthBarColors();
        }


        // -------------------- 测试接口：在编辑器中输入数值并点击按钮测试血量增减与血条最大值设置 --------------------


        [Export]
        LineEdit HpIncreaseText;
        /// <summary>
        /// 测试增加生命值
        /// </summary>
        public void TestHealthIncrease()
        {
            int value = UniversalUtil.StringToInt(HpIncreaseText.Text);
            HealthIncrease(value);
        }

        [Export]
        LineEdit HpReduceText;
        /// <summary>
        /// 测试减少生命值
        /// </summary>
        public void TestHealthReduce()
        {
            int value = UniversalUtil.StringToInt(HpReduceText.Text);
            HealthReduce(value);
        }


        [Export]
        LineEdit HpBarMaxText;
        /// <summary>
        /// 设置血条最大值
        /// </summary>
        /// <param name="value"></param>
        public void TestSetHpBarMax()
        {
            int value = UniversalUtil.StringToInt(HpBarMaxText.Text);
            SetHealthBarSingleValueMax(value);
        }


        [Export]
        LineEdit HpMaxText;
        /// <summary>
        /// 设置血条最大值
        /// </summary>
        /// <param name="value"></param>
        public void TestSetHpMax()
        {
            int value = UniversalUtil.StringToInt(HpMaxText.Text);
            SetHealthMax(value);
        }
    }
}