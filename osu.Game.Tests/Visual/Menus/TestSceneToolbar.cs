﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using Moq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Testing;
using osu.Game.Graphics.Containers;
using osu.Game.Overlays;
using osu.Game.Overlays.Toolbar;
using osu.Game.Rulesets;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Game.Tests.Visual.Menus
{
    [TestFixture]
    public class TestSceneToolbar : OsuManualInputManagerTestScene
    {
        private TestToolbar toolbar;

        [Resolved]
        private IRulesetStore rulesets { get; set; }

        private readonly Mock<INotificationOverlay> notifications = new Mock<INotificationOverlay>();

        private readonly BindableInt unreadNotificationCount = new BindableInt();

        [BackgroundDependencyLoader]
        private void load()
        {
            Dependencies.CacheAs(notifications.Object);
            notifications.SetupGet(n => n.UnreadCount).Returns(unreadNotificationCount);
        }

        [SetUp]
        public void SetUp() => Schedule(() =>
        {
            Child = toolbar = new TestToolbar { State = { Value = Visibility.Visible } };
        });

        [Test]
        public void TestNotificationCounter()
        {
            setNotifications(1);
            setNotifications(2);
            setNotifications(3);
            setNotifications(0);
            setNotifications(144);

            void setNotifications(int count)
                => AddStep($"set notification count to {count}",
                    () => unreadNotificationCount.Value = count);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestRulesetSwitchingShortcut(bool toolbarHidden)
        {
            ToolbarRulesetSelector rulesetSelector = null;

            if (toolbarHidden)
                AddStep("hide toolbar", () => toolbar.Hide());

            AddStep("retrieve ruleset selector", () => rulesetSelector = toolbar.ChildrenOfType<ToolbarRulesetSelector>().Single());

            for (int i = 0; i < 4; i++)
            {
                var expected = rulesets.AvailableRulesets.ElementAt(i);
                var numberKey = Key.Number1 + i;

                AddStep($"switch to ruleset {i} via shortcut", () =>
                {
                    InputManager.PressKey(Key.ControlLeft);
                    InputManager.Key(numberKey);
                    InputManager.ReleaseKey(Key.ControlLeft);
                });

                AddUntilStep("ruleset switched", () => rulesetSelector.Current.Value.Equals(expected));
            }
        }

        [TestCase(OverlayActivation.All)]
        [TestCase(OverlayActivation.Disabled)]
        public void TestRespectsOverlayActivation(OverlayActivation mode)
        {
            AddStep($"set activation mode to {mode}", () => toolbar.OverlayActivationMode.Value = mode);
            AddStep("hide toolbar", () => toolbar.Hide());
            AddStep("try to show toolbar", () => toolbar.Show());

            if (mode == OverlayActivation.Disabled)
                AddAssert("toolbar still hidden", () => toolbar.State.Value == Visibility.Hidden);
            else
                AddAssert("toolbar is visible", () => toolbar.State.Value == Visibility.Visible);
        }

        [Test]
        public void TestScrollInput()
        {
            OsuScrollContainer scroll = null;

            AddStep("add scroll layer", () => Add(scroll = new OsuScrollContainer
            {
                Depth = 1f,
                RelativeSizeAxes = Axes.Both,
                Child = new Box
                {
                    RelativeSizeAxes = Axes.X,
                    Height = DrawHeight * 2,
                    Colour = ColourInfo.GradientVertical(Color4.Gray, Color4.DarkGray),
                }
            }));

            AddStep("hover toolbar", () => InputManager.MoveMouseTo(toolbar));
            AddStep("perform scroll", () => InputManager.ScrollVerticalBy(500));
            AddAssert("not scrolled", () => scroll.Current == 0);
        }

        public class TestToolbar : Toolbar
        {
            public new Bindable<OverlayActivation> OverlayActivationMode => base.OverlayActivationMode as Bindable<OverlayActivation>;
        }
    }
}
