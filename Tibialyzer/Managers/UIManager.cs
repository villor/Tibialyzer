﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Tibialyzer {
    public class PageInfo {
        public bool prevPage = false;
        public bool nextPage = false;
        public int startDisplay = 0;
        public int endDisplay = 0;
        public int currentPage = 0;
        public PageInfo(bool prevPage, bool nextPage) {
            this.prevPage = prevPage;
            this.nextPage = nextPage;
        }
    }

    class UIManager {
        public static void Initialize() {
            CreateHelpMenu();
        }

        public static Pen pathPen = new Pen(StyleManager.PathFinderPathColor, 3);
        public static MapPictureBox DrawRoute(Coordinate begin, Coordinate end, Size pictureBoxSize, Size minSize, Size maxSize, List<Color> additionalWalkableColors, List<Target> targetList = null) {
            if (end.x >= 0 && begin.z != end.z) {
                throw new Exception("Can't draw route with different z-coordinates");
            }
            Rectangle sourceRectangle;
            MapPictureBox pictureBox = new MapPictureBox();
            if (pictureBoxSize.Width != 0) {
                pictureBox.Size = pictureBoxSize;
            }
            pictureBox.SizeMode = PictureBoxSizeMode.Zoom;

            if (targetList != null) {
                foreach (Target target in targetList) {
                    pictureBox.targets.Add(target);
                }
                if (end.x < 0) {
                    if (pictureBoxSize.Width == 0) {
                        pictureBoxSize = new Size(Math.Min(Math.Max(end.z, minSize.Width), maxSize.Width),
                            Math.Min(Math.Max(end.z, minSize.Height), maxSize.Height));
                        pictureBox.Size = pictureBoxSize;
                    }
                    Map map = StorageManager.getMap(begin.z);
                    pictureBox.map = map;
                    pictureBox.sourceWidth = end.z;
                    pictureBox.mapCoordinate = new Coordinate(begin.x, begin.y, begin.z);
                    pictureBox.zCoordinate = begin.z;
                    pictureBox.UpdateMap();
                    return pictureBox;
                }

            }

            // First find the route at a high level
            Node beginNode = Pathfinder.GetNode(begin.x, begin.y, begin.z);
            Node endNode = Pathfinder.GetNode(end.x, end.y, end.z);

            List<Rectangle> collisionBounds = null;
            DijkstraNode highresult = Dijkstra.FindRoute(beginNode, endNode);
            if (highresult != null) {
                collisionBounds = new List<Rectangle>();
                while (highresult != null) {
                    highresult.rect.Inflate(5, 5);
                    collisionBounds.Add(highresult.rect);
                    highresult = highresult.previous;
                }
                if (collisionBounds.Count == 0) collisionBounds = null;
            }

            Map m = StorageManager.getMap(begin.z);
            DijkstraPoint result = Dijkstra.FindRoute(m.image, new Point(begin.x, begin.y), new Point(end.x, end.y), collisionBounds, additionalWalkableColors);
            if (result == null) {
                throw new Exception("Couldn't find route.");
            }

            // create a rectangle from the result
            double minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            DijkstraPoint node = result;
            while (node != null) {
                if (node.point.X < minX) minX = node.point.X;
                if (node.point.Y < minY) minY = node.point.Y;
                if (node.point.X > maxX) maxX = node.point.X;
                if (node.point.Y > maxY) maxY = node.point.Y;
                node = node.previous;
            }

            minX -= 10;
            minY -= 10;
            maxX += 10;
            maxY += 10;

            int size = (int)Math.Max(maxX - minX, maxY - minY);
            sourceRectangle = new Rectangle((int)minX, (int)minY, size, size);
            if (pictureBoxSize.Width == 0) {
                pictureBoxSize = new Size(Math.Min(Math.Max(sourceRectangle.Width, minSize.Width), maxSize.Width),
                    Math.Min(Math.Max(sourceRectangle.Height, minSize.Height), maxSize.Height));
                pictureBox.Size = pictureBoxSize;
            }
            TibiaPath path = new TibiaPath();
            path.begin = new Coordinate(begin);
            path.end = new Coordinate(end);
            path.path = result;
            pictureBox.paths.Add(path);

            pictureBox.map = m;
            pictureBox.sourceWidth = size;
            pictureBox.mapCoordinate = new Coordinate(sourceRectangle.X + sourceRectangle.Width / 2, sourceRectangle.Y + sourceRectangle.Height / 2, begin.z);
            pictureBox.zCoordinate = begin.z;
            pictureBox.UpdateMap();

            return pictureBox;
        }

        enum HeaderType { Numeric = 0, String = 1 };
        private static IComparable CoerceTypes(IComparable value, HeaderType type) {
            if (type == HeaderType.Numeric) {
                string valueString = value.ToString();
                double dblVal;
                if (double.TryParse(valueString, NumberStyles.Any, CultureInfo.InvariantCulture, out dblVal)) {
                    return dblVal;
                }
                return (double)-127;
            } else if (type == HeaderType.String) {
                return value.ToString();
            }
            return value;
        }

        public static int DisplayCreatureAttributeList(System.Windows.Forms.Control.ControlCollection controls, List<TibiaObject> l, int base_x, int base_y, out int maxwidth, Func<TibiaObject, string> tooltip_function = null, List<Control> createdControls = null, int page = 0, int pageitems = 20, PageInfo pageInfo = null, string extraAttribute = null, Func<TibiaObject, Attribute> attributeFunction = null, EventHandler headerSortFunction = null, string sortedHeader = null, bool desc = false, Func<TibiaObject, IComparable> extraSort = null, List<string> removedAttributes = null, bool conditional = false) {
            const int size = 24;
            const int imageSize = size - 4;
            // add a tooltip that displays the creature names
            ToolTip value_tooltip = new ToolTip();
            value_tooltip.AutoPopDelay = 60000;
            value_tooltip.InitialDelay = 500;
            value_tooltip.ReshowDelay = 0;
            value_tooltip.ShowAlways = true;
            value_tooltip.UseFading = true;
            int currentPage = 0;
            if (pageInfo != null) {
                pageInfo.prevPage = page > 0;
            }
            int offset = 0;
            if (sortedHeader != "" && sortedHeader != null) {
                int hash = sortedHeader.GetHashCode();
                HeaderType type = HeaderType.String;
                foreach (TibiaObject obj in l) {
                    List<string> headers = conditional ? obj.GetConditionalHeaders() : obj.GetAttributeHeaders();
                    if (headers.Contains(sortedHeader)) {
                        IComparable value = conditional ? obj.GetConditionalHeaderValue(sortedHeader) : obj.GetHeaderValue(hash);
                        if (value is string) {
                            type = HeaderType.String;
                        } else {
                            type = HeaderType.Numeric;
                        }
                        break;
                    }
                }

                if (desc) {
                    if (sortedHeader == extraAttribute && extraSort != null) {
                        l = l.OrderByDescending(o => extraSort(o)).ToList();
                    } else {
                        l = l.OrderByDescending(o => CoerceTypes(conditional ? o.GetConditionalHeaderValue(sortedHeader) : o.GetHeaderValue(hash), type)).ToList();
                    }
                } else {
                    if (sortedHeader == extraAttribute && extraSort != null) {
                        l = l.OrderBy(o => extraSort(o)).ToList();
                    } else {
                        l = l.OrderBy(o => CoerceTypes(conditional ? o.GetConditionalHeaderValue(sortedHeader) : o.GetHeaderValue(hash), type)).ToList();
                    }
                }
            }
            int start = 0;
            List<TibiaObject> pageItems = new List<TibiaObject>();
            Dictionary<string, int> totalAttributes = new Dictionary<string, int>();
            foreach (TibiaObject cr in l) {
                if (offset > pageitems) {
                    if (page > currentPage) {
                        offset = 0;
                        currentPage += 1;
                    } else {
                        if (pageInfo != null) {
                            pageInfo.nextPage = true;
                        }
                        break;
                    }
                }
                if (currentPage == page) {
                    pageItems.Add(cr);
                } else {
                    start++;
                }
                offset++;
            }
            if (pageInfo != null) {
                pageInfo.startDisplay = start;
                pageInfo.endDisplay = start + pageItems.Count;
            }
            Dictionary<string, double> sortValues = new Dictionary<string, double>();
            foreach (TibiaObject obj in conditional ? l : pageItems) {
                List<string> headers = conditional ? obj.GetConditionalHeaders() : new List<string>(obj.GetAttributeHeaders());
                List<Attribute> attributes = conditional ? obj.GetConditionalAttributes() : obj.GetAttributes();
                if (extraAttribute != null) {
                    headers.Add(extraAttribute);
                    attributes.Add(attributeFunction(obj));
                }
                for (int i = 0; i < headers.Count; i++) {
                    string header = headers[i];
                    Attribute attribute = attributes[i];
                    if (!sortValues.ContainsKey(header)) {
                        sortValues.Add(header, i);
                    } else {
                        sortValues[header] = Math.Max(sortValues[header], i);
                    }
                    if (removedAttributes != null && removedAttributes.Contains(header)) continue;
                    int width = TextRenderer.MeasureText(header, StyleManager.TextFont).Width + 10;
                    if (attribute is StringAttribute || attribute is CommandAttribute) {
                        string text = attribute is StringAttribute ? (attribute as StringAttribute).value : (attribute as CommandAttribute).value;
                        width = Math.Max(TextRenderer.MeasureText(text, StyleManager.TextFont).Width, width);
                    } else if (attribute is ImageAttribute) {
                        width = Math.Max((attribute as ImageAttribute).value == null ? 0 : (attribute as ImageAttribute).value.Width, width);
                    } else if (attribute is BooleanAttribute) {
                        width = Math.Max(20, width);
                    } else {
                        throw new Exception("Unrecognized attribute.");
                    }
                    width = Math.Min(width, attribute.MaxWidth);
                    if (!totalAttributes.ContainsKey(header)) {
                        int headerWidth = TextRenderer.MeasureText(header, StyleManager.TextFont).Width;
                        totalAttributes.Add(header, Math.Max(headerWidth, width));
                    } else if (totalAttributes[header] < width) {
                        totalAttributes[header] = width;
                    }
                }
            }
            base_x += 24;
            maxwidth = base_x;
            List<string> keys = totalAttributes.Keys.ToList();
            if (conditional) {
                keys = keys.OrderBy(o => sortValues[o]).ToList();
            }
            // create header information
            int x = base_x;
            foreach (string k in keys) {
                int val = totalAttributes[k];
                Label label = new Label();
                label.Name = k;
                label.Text = k;
                label.Location = new Point(x, base_y);
                label.ForeColor = StyleManager.NotificationTextColor;
                label.Size = new Size(val, size);
                label.Font = StyleManager.TextFont;
                label.BackColor = Color.Transparent;
                label.TextAlign = ContentAlignment.MiddleCenter;
                label.BorderStyle = BorderStyle.FixedSingle;
                if (headerSortFunction != null)
                    label.Click += headerSortFunction;
                controls.Add(label);
                if (createdControls != null) {
                    createdControls.Add(label);
                }
                x += val;
                maxwidth += val;
            }
            maxwidth += 10;
            offset = 0;

            // create object information
            foreach (TibiaObject obj in pageItems) {
                List<string> headers = conditional ? obj.GetConditionalHeaders() : new List<string>(obj.GetAttributeHeaders());
                List<Attribute> attributes = conditional ? obj.GetConditionalAttributes() : obj.GetAttributes();
                if (extraAttribute != null) {
                    headers.Add(extraAttribute);
                    attributes.Add(attributeFunction(obj));
                }
                string command = obj.GetCommand();

                // Every row is rendered on a single picture box for performance reasons
                PictureBox picture;
                picture = new PictureBox();
                picture.Image = obj.GetImage();
                picture.Size = new Size(imageSize, imageSize);
                picture.SizeMode = PictureBoxSizeMode.Zoom;
                picture.Location = new Point(base_x - 24, size * (offset + 1) + base_y);
                picture.BackColor = Color.Transparent;
                if (obj.AsItem() != null) {
                    picture.BackgroundImage = StyleManager.GetImage("item_background.png");
                }
                if (createdControls != null) {
                    createdControls.Add(picture);
                }
                controls.Add(picture);
                if (tooltip_function == null) {
                    if (obj.AsItem() != null) {
                        value_tooltip.SetToolTip(picture, obj.AsItem().look_text);
                    } else {
                        value_tooltip.SetToolTip(picture, obj.GetName());
                    }
                } else {
                    value_tooltip.SetToolTip(picture, tooltip_function(obj));
                }
                x = base_x;
                foreach (string k in keys) {
                    int val = totalAttributes[k];
                    int index = headers.IndexOf(k);
                    if (index < 0) {
                        x += val;
                        continue;
                    }
                    Attribute attribute = attributes[index];
                    Control c;
                    if (attribute is StringAttribute || attribute is CommandAttribute) {
                        string text = attribute is StringAttribute ? (attribute as StringAttribute).value : (attribute as CommandAttribute).value;
                        Color color = attribute is StringAttribute ? (attribute as StringAttribute).color : (attribute as CommandAttribute).color;
                        // create label
                        Label label = new Label();
                        label.Text = text;
                        label.ForeColor = color;
                        label.Size = new Size(val, size);
                        label.Font = StyleManager.TextFont;
                        label.Location = new Point(x, size * (offset + 1) + base_y);
                        label.BackColor = Color.Transparent;
                        if (createdControls != null) {
                            createdControls.Add(label);
                        }
                        controls.Add(label);
                        c = label;
                    } else if (attribute is ImageAttribute || attribute is BooleanAttribute) {
                        // create picturebox
                        picture = new PictureBox();
                        picture.Image = (attribute is ImageAttribute) ? (attribute as ImageAttribute).value : ((attribute as BooleanAttribute).value ? StyleManager.GetImage("checkmark-yes.png") : StyleManager.GetImage("checkmark-no.png"));
                        picture.Size = new Size(imageSize, imageSize);
                        picture.SizeMode = PictureBoxSizeMode.Zoom;
                        picture.Location = new Point(x + (val - imageSize) / 2, size * (offset + 1) + base_y);
                        picture.BackColor = Color.Transparent;
                        if (createdControls != null) {
                            createdControls.Add(picture);
                        }
                        controls.Add(picture);
                        c = picture;
                    } else {
                        throw new Exception("Unrecognized attribute.");
                    }
                    if (attribute is CommandAttribute) {
                        c.Name = (attribute as CommandAttribute).command;
                    } else {
                        c.Name = obj.GetCommand();
                    }
                    c.Click += executeNameCommand;
                    if (tooltip_function == null) {
                        if (attribute is StringAttribute || attribute is CommandAttribute) {
                            string text = attribute is StringAttribute ? (attribute as StringAttribute).value : (attribute as CommandAttribute).value;
                            value_tooltip.SetToolTip(c, text);
                        } else {
                            value_tooltip.SetToolTip(c, obj.GetName());
                        }
                    } else {
                        value_tooltip.SetToolTip(c, tooltip_function(obj));
                    }
                    x += val;
                }

                offset++;
            }
            return (offset + 1) * size;
        }

        public static int DisplayCreatureList(System.Windows.Forms.Control.ControlCollection controls, List<TibiaObject> l, int base_x, int base_y, int max_x, int spacing, Func<TibiaObject, string> tooltip_function = null, float magnification = 1.0f, List<Control> createdControls = null, int page = 0, int pageheight = 10000, PageInfo pageInfo = null, int currentDisplay = -1) {
            int x = 0, y = 0;
            int height = 0;
            // add a tooltip that displays the creature names
            ToolTip value_tooltip = new ToolTip();
            value_tooltip.AutoPopDelay = 60000;
            value_tooltip.InitialDelay = 500;
            value_tooltip.ReshowDelay = 0;
            value_tooltip.ShowAlways = true;
            value_tooltip.UseFading = true;
            int currentPage = 0;
            if (pageInfo != null) {
                pageInfo.prevPage = page > 0;
            }
            int start = 0, end = 0;
            int pageStart = 0;
            if (currentDisplay >= 0) {
                page = int.MaxValue;
            }
            for (int i = 0; i < l.Count; i++) {
                TibiaObject cr = l[i];
                int imageWidth;
                int imageHeight;
                Image image = cr.GetImage();
                string name = cr.GetName();

                if (cr.AsItem() != null || cr.AsSpell() != null) {
                    imageWidth = 32;
                    imageHeight = 32;
                } else {
                    imageWidth = image.Width;
                    imageHeight = image.Height;
                }

                if (currentDisplay >= 0 && i == currentDisplay) {
                    currentDisplay = -1;
                    i = pageStart;
                    start = i;
                    page = currentPage;
                    pageInfo.prevPage = page > 0;
                    pageInfo.currentPage = page;
                    x = 0;
                    y = 0;
                    continue;
                }

                if (max_x < (x + base_x + (int)(imageWidth * magnification) + spacing)) {
                    x = 0;
                    y = y + spacing + height;
                    height = 0;
                    if (y > pageheight) {
                        if (page > currentPage) {
                            y = 0;
                            currentPage += 1;
                            pageStart = start;
                        } else {
                            if (pageInfo != null) {
                                pageInfo.nextPage = true;
                            }
                            break;
                        }
                    }
                }
                if ((int)(imageHeight * magnification) > height) {
                    height = (int)(imageHeight * magnification);
                }
                if (currentPage == page) {
                    PictureBox image_box;
                    image_box = new PictureBox();
                    image_box.Image = image;
                    image_box.BackColor = Color.Transparent;
                    image_box.Size = new Size((int)(imageWidth * magnification), height);
                    image_box.Location = new Point(base_x + x, base_y + y);
                    image_box.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
                    image_box.Name = cr.GetCommand();
                    image_box.Click += executeNameCommand;
                    if (cr.AsItem() != null) {
                        image_box.BackgroundImage = StyleManager.GetImage("item_background.png");
                    }
                    controls.Add(image_box);
                    if (createdControls != null) createdControls.Add(image_box);
                    image_box.Image = image;
                    if (tooltip_function == null) {
                        value_tooltip.SetToolTip(image_box, name.ToTitle());
                    } else {
                        string prefix = "";
                        if (cr.AsNPC() != null) {
                            NPC npc = cr is NPC ? cr as NPC : (cr as LazyTibiaObject).getTibiaObject() as NPC;
                            prefix = name.ToTitle() + " (" + npc.city.ToTitle() + ")\n";
                        }
                        value_tooltip.SetToolTip(image_box, prefix + tooltip_function(cr));
                    }
                    end++;
                } else {
                    start++;
                }

                x = x + (int)(imageWidth * magnification) + spacing;
            }
            if (pageInfo != null) {
                pageInfo.startDisplay = start;
                pageInfo.endDisplay = start + end;
            }
            x = 0;
            y = y + height;
            return y;
        }

        private static void executeNameCommand(object sender, EventArgs e) {
            CommandManager.ExecuteCommand((sender as Control).Name);
        }

        public static ToolTip CreateTooltip() {
            ToolTip tooltip = new ToolTip();
            tooltip.AutoPopDelay = 60000;
            tooltip.InitialDelay = 500;
            tooltip.ReshowDelay = 0;
            tooltip.ShowAlways = true;
            tooltip.UseFading = true;
            return tooltip;
        }

        private static List<Control> helpMenu = new List<Control>();
        private static void CreateHelpMenu() {
            Label label;
            label = new Label();
            label.BackColor = StyleManager.MainFormButtonColor;
            label.Font = StyleManager.MainFormLabelFontSmall;
            label.ForeColor = StyleManager.MainFormButtonForeColor;
            label.Size = new System.Drawing.Size(170, 15);
            label.Text = "Enter: New Item";
            label.Name = "newitem";
            label.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            helpMenu.Add(label);
            label = new Label();
            label.BackColor = StyleManager.MainFormButtonColor;
            label.Font = StyleManager.MainFormLabelFontSmall;
            label.ForeColor = StyleManager.MainFormButtonForeColor;
            label.Size = new System.Drawing.Size(170, 15);
            label.Text = "Delete: Delete Item";
            label.Name = "deleteitem";
            label.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            helpMenu.Add(label);
            label = new Label();
            label.BackColor = StyleManager.MainFormButtonColor;
            label.Font = StyleManager.MainFormLabelFontSmall;
            label.ForeColor = StyleManager.MainFormButtonForeColor;
            label.Size = new System.Drawing.Size(170, 15);
            label.Text = "Ctrl+Backsp: Erase Item";
            label.Name = "modifyitem";
            label.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            helpMenu.Add(label);
            label = new Label();
            label.BackColor = StyleManager.MainFormButtonColor;
            label.Font = StyleManager.MainFormLabelFontSmall;
            label.ForeColor = StyleManager.MainFormButtonForeColor;
            label.Size = new System.Drawing.Size(170, 15);
            label.Text = "Ctrl+C/V: Copy/Paste";
            label.Name = "modifyitem";
            label.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            helpMenu.Add(label);
        }

        public static List<Control> GetHelpMenu() {
            return helpMenu;
        }
    }
}
