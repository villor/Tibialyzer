﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;

namespace Tibialyzer {
    class PrettyCheckBox : CheckBox{

        public PrettyCheckBox() {
            this.SetStyle(ControlStyles.UserPaint, true);
            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
        }

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            int paddingX = Padding.Left / 2;
            int paddingY = Padding.Top / 2;
            e.Graphics.FillRectangle(new SolidBrush(BackColor), new Rectangle(paddingX, 8, 16 + paddingX, 16 + paddingX));
            if (!StyleManager.Initialized) {
                e.Graphics.FillRectangle(new SolidBrush(Color.Red), new Rectangle(paddingX, 8, 16 + paddingX, 16 + paddingX));
            } else {
                if (this.Checked) {
                    e.Graphics.DrawImage(StyleManager.GetImage("checkbox-checked.png"), new Rectangle(paddingX, 8, 16 + paddingX, 16 + paddingX));
                } else {
                    e.Graphics.DrawImage(StyleManager.GetImage("checkbox-empty.png"), new Rectangle(paddingX, 8, 16 + paddingX, 16 + paddingX));
                }
            }
        }
    }
}
