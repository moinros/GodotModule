using Godot;
using Moinros.CSharp.Util;
using System;

namespace MetalLimit.Project.Scene.Module.Part.HealthBar
{

    /// <summary>
    /// 血条组件：支持多条血条、血量增加时极速填充动画、血量减少时过渡色块动画、颜色轮换规则、血量显示
    /// </summary>
    public partial class HealthBar : Control
    {

        [Export]
        [ExportGroup("HealthBar")]
        /// <summary>
        /// 默认血条颜色配置（可在编辑器中修改）
        /// 注: 颜色按血条数量循环：1 条血 = 首位颜色，每多一条血推进一个颜色，循环完一轮回到首位颜色重新开始
        /// </summary>
        private Color[] DefaultColors = [
            Color.Color8(255, 0, 0) ,
            Color.Color8(240, 160, 40),
            Color.Color8(0, 180, 0),
            Color.Color8(40, 120, 240),
            Color.Color8(60, 80, 200)
        ];

        /// <summary>
        /// 当前血条颜色配置
        /// </summary>
        private Color[] CurrentColors;

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
        private void ShowHealthBarCount(int value)
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
        /// 过渡动画颜色衰减时长（秒）：受击后失去血量区间的色块由亮变深的倒计时
        /// </summary>
        [Export]
        float TransitionDuration = 0.4f;

        /// <summary>
        /// 过渡动画起始颜色（亮白）
        /// </summary>
        [Export]
        Color TransitionColorBright = new(1f, 1f, 1f, 0.8f);

        /// <summary>
        /// 过渡动画结束颜色（暗色）：倒计时结束后色块保持此颜色进入收缩阶段，直至缩短到消失
        /// </summary>
        [Export]
        Color TransitionColorDark = new(0.55f, 0f, 0f, 0.8f);

        /// <summary>
        /// 过渡色块收缩速度（血量/秒）：颜色衰减倒计时结束后，色块从右向左以该速度缩短
        /// （类似加血填充血条的相反效果），直至缩短到消失；默认 500，单条血量 1000 时约 2 秒收缩一整条
        /// </summary>
        [Export]
        float TransitionShrinkSpeed = 500f;

        /// <summary>
        /// 血量增加时的填充动画时长（秒）：血条以极快速度填充到新血量值
        /// </summary>
        [Export]
        float HealDuration = 0.2f;

        /// <summary>
        /// 一个正在播放的过渡色块（受击后失去血量区间的“颜色衰减 + 收缩消失”两阶段动画）
        /// </summary>
        private sealed class TransitionChunk
        {
            /// <summary>色块节点（最上层血条的子节点，绘制在血条填充之下，只露出失去的血量部分）</summary>
            public ColorRect Rect;
            /// <summary>已播放时间（秒，仅颜色衰减倒计时阶段使用）</summary>
            public float Elapsed;
            /// <summary>颜色衰减倒计时时长（秒）</summary>
            public float Duration;
            /// <summary>起始颜色（亮色）</summary>
            public Color StartColor;
            /// <summary>结束颜色（暗色，收缩阶段保持此颜色直至缩短到消失）</summary>
            public Color EndColor;
            /// <summary>是否已进入收缩阶段：倒计时结束后为 true，色块从右向左缩短直至消失</summary>
            public bool Shrinking;
            /// <summary>色块左端对应的血量值（收缩的终点）</summary>
            public double LeftValue;
            /// <summary>色块右端对应的血量值（收缩阶段逐帧递减）</summary>
            public double RightValue;
        }

        /// <summary>
        /// 正在播放的过渡色块（短时间内多次受击时各自独立共存）
        /// </summary>
        private readonly LinkList<TransitionChunk> _activeChunks = new();

        public override void _Ready()
        {
            base._Ready();
            // 初始化颜色配置：复制一份导出的默认颜色作为运行时配置（修改运行时颜色不影响导出值）
            if (DefaultColors != null && DefaultColors.Length > 0)
            {
                CurrentColors = new Color[DefaultColors.Length];
                Array.Copy(DefaultColors, CurrentColors, DefaultColors.Length);
            }
            else
            {
                // 导出数组被清空（或为 null）时兜底为红色，保证后续取色不越界
                CurrentColors = [Colors.Red];
            }

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
            // 推进所有过渡色块的颜色衰减与收缩（各色块独立计时，互不影响，可共存）
            UpdateTransitionChunks((float)delta);
        }

        /// <summary>
        /// 设置血条颜色配置
        /// 注: 颜色按血条数量循环：1 条血 = 首位颜色，每多一条血推进一个颜色，循环完一轮回到首位颜色重新开始
        /// </summary>
        private void SetHealthBarColors(Color[] colors)
        {
            if (colors != null && colors.Length > 0)
            {
                // 复制传入数组：外部后续修改原数组不会影响血条显示
                CurrentColors = (Color[])colors.Clone();
            }
            else if (DefaultColors != null && DefaultColors.Length > 0)
            {
                // 传空重置为导出默认颜色的副本（保持“运行时配置是副本”的语义，不直接引用导出数组）
                CurrentColors = (Color[])DefaultColors.Clone();
            }
            else
            {
                // 导出默认配置也为空时兜底红色
                CurrentColors = [Colors.Red];
            }
            // 颜色配置变化后按当前血条计数重新计算颜色索引，再刷新显示
            SetColorIndex(GetColorIndexFromBarCount(HealthBarCurrentCount));
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
            // 颜色配置为空时无从取色：下层条置透明并跳过本次刷新，避免 Clamp(0, 0, -1) 得 -1 越界
            if (CurrentColors == null || CurrentColors.Length == 0)
            {
                HealthBarUnderColor.Color = Colors.Transparent;
                return;
            }

            // 更新上层血条填充颜色
            _upperFillStyle.BgColor = CurrentColors[Mathf.Clamp(_colorIndex, 0, CurrentColors.Length - 1)];

            // 更新下层血条颜色：显示下一根（少一条）血条的颜色
            if (barCount > 1)
            {
                // GetColorIndexFromBarCount 保证返回有效索引，无需额外夹紧
                int underColorIndex = GetColorIndexFromBarCount(barCount - 1);
                HealthBarUnderColor.Color = CurrentColors[underColorIndex];
            }
            else
            {
                HealthBarUnderColor.Color = Colors.Transparent;
            }
        }

        /// <summary>
        /// 获取当前血条颜色配置
        /// </summary>
        private Color[] GetHealthBarColors()
        {
            // 尚未初始化（_Ready 之前）或配置为空时返回空数组
            if (CurrentColors == null || CurrentColors.Length == 0)
            {
                return [];
            }
            var colors = new Color[CurrentColors.Length];
            Array.Copy(CurrentColors, colors, CurrentColors.Length);
            return colors;
        }

        /// <summary>
        /// 获取指定索引的血条颜色
        /// </summary>
        private Color GetHealthBarColor(int index)
        {
            // 配置为空时兜底红色；越界时返回当前生效配置的首色（而非导出默认值）
            if (CurrentColors == null || CurrentColors.Length == 0)
            {
                return Colors.Red;
            }
            if (index < 0 || index >= CurrentColors.Length)
            {
                return CurrentColors[0];
            }
            return CurrentColors[index];
        }

        /// <summary>
        /// 设置指定索引的血条颜色
        /// </summary>
        private void SetHealthBarColor(int index, Color color)
        {
            if (CurrentColors != null && index >= 0 && index < CurrentColors.Length)
            {
                CurrentColors[index] = color;
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

                // 在失去的血量区间上生成过渡色块（由亮变深后再从右向左收缩到消失，不使用进度条）
                // 掉整条（或多条）时色块延伸到满条位置；条内扣血时延伸到更新前的值
                int lostFrom = valueInBar;
                int lostTo = HealthBarCurrentCount < _lastBarCount ? HealthBarSingleValueMax : previousValueInBar;
                if (lostTo > lostFrom)
                {
                    SpawnTransitionChunk(lostFrom, lostTo);
                }

                // 根据剩余血条数切换颜色（全部颜色按血条数量循环轮换，最后一条血必定为首位颜色红色）
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
        /// 色块先由亮变深播完颜色衰减倒计时，再从右向左收缩到零，最后自动销毁
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
                // 结束态：颜色变深且保持可见，消失由收缩阶段完成
                EndColor = TransitionColorDark,
                Shrinking = false,
                LeftValue = fromValue,
                RightValue = toValue,
            });
        }

        /// <summary>
        /// 逐帧推进所有过渡色块的两阶段动画：
        /// 阶段一（颜色衰减倒计时）：色块由亮变深；
        /// 阶段二（收缩）：倒计时结束后不再直接消失，而是从右向左以指定速度缩短
        /// </summary>
        private void UpdateTransitionChunks(float delta)
        {
            // 收缩速度下限 1（血量/秒），避免 0 或负值导致色块永远无法收缩到消失
            float shrinkSpeed = Mathf.Max(TransitionShrinkSpeed, 1f);

            TransitionChunk chunk = _activeChunks.Next();
            while (chunk != null)
            {
                if (chunk.Shrinking)
                {
                    // 收缩阶段：右端以指定速度向左推进
                    ShrinkTransitionChunk(chunk, delta, shrinkSpeed);
                }
                else
                {
                    chunk.Elapsed += delta;
                    float t = chunk.Elapsed / chunk.Duration;
                    if (t < 1f)
                    {
                        // 颜色衰减阶段：由亮变深
                        chunk.Rect.Color = chunk.StartColor.Lerp(chunk.EndColor, t);
                    }
                    else
                    {
                        // 倒计时结束：定格为结束颜色并进入收缩阶段，倒计时超出的时间计入收缩
                        chunk.Rect.Color = chunk.EndColor;
                        chunk.Shrinking = true;
                        ShrinkTransitionChunk(chunk, chunk.Elapsed - chunk.Duration, shrinkSpeed);
                    }
                }
                chunk = _activeChunks.Next();
            }

        }

        /// <summary>
        /// 按时长收缩单个过渡色块：右端血量值以指定速度递减（从右向左缩短，类似加血填充的相反效果），
        /// 缩短到左端（宽度为零）时销毁色块；否则按血量比例更新右端锚点，保持跟随血条尺寸变化
        /// </summary>
        private void ShrinkTransitionChunk(TransitionChunk chunk, float seconds, float shrinkSpeed)
        {
            if (seconds <= 0f)
            {
                return;
            }

            chunk.RightValue -= shrinkSpeed * seconds;
            if (chunk.RightValue <= chunk.LeftValue)
            {
                // 已缩短到零：销毁色块
                chunk.Rect.QueueFree();
                _activeChunks.Remove(chunk);
            }
            else
            {
                // 用锚点按血量比例更新右端位置：色块自动跟随血条尺寸变化
                chunk.Rect.SetAnchorAndOffset(Side.Right, (float)(chunk.RightValue / HealthBarSingleValueMax), 0);
            }
        }

        /// <summary>
        /// 立即清除所有正在播放的过渡色块（加血时不保留扣血残影）
        /// </summary>
        private void ClearTransitionChunks()
        {
            // Remove 会把游标回缩到被删节点的前驱，Next() 随后重新定位到后继，
            // 因此该循环本身即可清空整条链表，无需再调用 RemoveAll()
            TransitionChunk chunk = _activeChunks.Next();
            while (chunk != null)
            {
                chunk.Rect.QueueFree();
                _activeChunks.Remove(chunk);
                chunk = _activeChunks.Next();
            }
        }

        /// <summary>
        /// 设置过渡动画时长（秒）；只影响之后生成的过渡色块，正在播放的不受影响
        /// </summary>
        public void SetTransitionDuration(float seconds)
        {
            TransitionDuration = Mathf.Max(seconds, 0.01f);
        }

        /// <summary>
        /// 设置过渡色块收缩速度（血量/秒，最小 1）；只影响之后生成的过渡色块，正在播放的不受影响
        /// </summary>
        public void SetTransitionShrinkSpeed(float speed)
        {
            TransitionShrinkSpeed = Mathf.Max(speed, 1f);
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
        /// 注: 单根血条最大值变化会同步重算总血条数与当前血条计数，并刷新颜色与计数显示
        /// </summary>
        public void SetHealthBarSingleValueMax(int value)
        {
            // 至少为 1，避免 0/负值导致取模与除零异常
            HealthBarSingleValueMax = Mathf.Max(value, 1);
            HealthBarUpper.MaxValue = HealthBarSingleValueMax;

            // 同步重算总血条数与当前血条计数：
            HealthBarTotalCount = (int)Math.Ceiling((double)HealthMax / HealthBarSingleValueMax);
            HealthBarCurrentCount = GetHealthBarCount();
            ShowHealthBarCount(HealthBarCurrentCount);

            // 中断进行中的填充动画并直接定位到当前血量，避免条内值映射变化导致显示错位
            CancelHealAnimation();
            // 清除仍在播放的过渡色块：其血量值与锚点按旧的单条最大值计算，换算后会错位
            ClearTransitionChunks();
            HealthBarUpper.Value = GetBarValueInBar(HealthValue);

            // 重置掉整条判断基准：单根最大值变化属于配置刷新，不视为掉条
            _lastBarCount = HealthBarCurrentCount;

            // 按新的血条计数刷新颜色显示（上层条填充色、下层条下一根颜色）
            SetColorIndex(GetColorIndexFromBarCount(HealthBarCurrentCount));
        }

        /// <summary>
        /// 设置最大生命值
        /// 若最大值小于当前血量：同步夹紧当前血量，并刷新血条颜色、计数与进度显示
        /// </summary>
        public void SetHealthMax(int value)
        {
            // 至少为 0：负的最大值会让 Mathf.Clamp(value, 0, max) 反向得出负血量
            HealthMax = Mathf.Max(value, 0);
            HealthMaxLabel.Text = HealthMax.ToString();
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
        /// 设置当前生命值（传入绝对值）
        /// 血条直接切换到对应的颜色：血量减少触发过渡色块动画，血量增加触发极速填充动画
        /// </summary>
        public void SetHealthValue(int value)
        {
            ApplyHealthValue(value);
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
        /// 全部颜色按血条数量循环：1 条血 = 首位颜色（默认红色），每多一条血推进一个颜色，
        /// 循环完一轮回到首位颜色重新开始（5 色示例：1=红、2=黄、3=绿、4=蓝、5=紫、6=红、7=黄……）
        /// </summary>
        private int GetColorIndexFromBarCount(int barCount)
        {
            // 最后一条血（及空血条）必定使用第一个颜色（默认红色）
            if (barCount <= 1)
            {
                return 0;
            }
            // 颜色配置为空时无从取色，防御性返回首色索引
            if (CurrentColors == null || CurrentColors.Length == 0)
            {
                return 0;
            }
            // 直接按血条数量在全部颜色中循环：血条数减 1 即颜色索引
            return (barCount - 1) % CurrentColors.Length;
        }

        /// <summary>
        /// 设置血条颜色索引
        /// </summary>
        private void SetColorIndex(int index)
        {
            // 配置为空时保持索引 0，避免 Clamp(x, 0, -1) 得到 -1 污染索引导致后续取色越界
            _colorIndex = (CurrentColors == null || CurrentColors.Length == 0) ? 0 : Mathf.Clamp(index, 0, CurrentColors.Length - 1);
            // 更新颜色显示（含下层血条显示的下一根颜色）
            UpdateHealthBarColors();
        }


        /// -------------------- 以下为测试用：在编辑器中输入数值并点击按钮测试血量增减与血条最大值设置 --------------------


        [Export]
        [ExportGroup("Test")]
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