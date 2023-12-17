using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ParameterManager
{
    class TreeViewHelper
    {
        public static void SelectDown(IBaseModel baseModel, bool newValue)
        {
            baseModel.IsCheck = newValue;
            baseModel.IsExpand = newValue;

            if (baseModel.TreeNodes != null)//类别节点
            {
                foreach (var node in baseModel.TreeNodes)
                {
                    if (node.IsCheck != newValue && node.NodeVisibility == Visibility.Visible)//仅选择当前可见节点
                    {
                        TreeViewHelper.SelectDown(node, newValue);
                    }
                }
            }
        }

        public static void SelectUp(IBaseModel baseModel,bool treeAllIsCheck, IEnumerable<CatagoryTreeNode> rootTreeModels)
        {
            if (baseModel.Parent == null)
            {
                //是否全选判断
                treeAllIsCheck = true;
                foreach (var peerNode in rootTreeModels)
                {
                    if (peerNode.IsCheck != true)
                    {
                        treeAllIsCheck = false;
                        break;
                    }
                }
                return;
            }
            if (baseModel.IsCheck != null)
            {
                baseModel.Parent.IsCheck = baseModel.IsCheck;
                foreach (var peerNode in baseModel.Parent.TreeNodes)
                {
                    if (peerNode.IsCheck != baseModel.IsCheck)
                    {
                        baseModel.Parent.IsCheck = null;
                        break;
                    }
                }
                if (baseModel.Parent.IsCheck == false)//若其子节点都未选择，则折叠该节点
                {
                    baseModel.Parent.IsExpand = false;
                }
            }
            else
            {
                baseModel.Parent.IsCheck = null;
            }
            TreeViewHelper.SelectUp(baseModel.Parent, treeAllIsCheck, rootTreeModels);
        }

        /// 递归遍历树
        public static void RecursiveTraverseTree(IBaseModel baseModel, Action<IBaseModel> action)
        {
            action(baseModel);
            if (baseModel.TreeNodes == null)
            {
                return;
            }
            baseModel.TreeNodes.ToList().ForEach(x =>
            {
                TreeViewHelper.RecursiveTraverseTree(x, action);
            });
        }

        public static void ShowSearchUp(IBaseModel baseModel)
        {
            if (baseModel.Parent == null)
            {
                return;
            }
            baseModel.Parent.NodeVisibility = Visibility.Visible;
            baseModel.Parent.IsExpand = true;
            TreeViewHelper.ShowSearchUp(baseModel.Parent);
        }

        public static void ShowSearchDown(IBaseModel baseModel)
        {
            if (baseModel.TreeNodes == null)
            {
                return;
            }
            baseModel.TreeNodes.ToList().ForEach(x =>
            {
                x.NodeVisibility = Visibility.Visible;
                x.IsExpand = true;
                TreeViewHelper.ShowSearchDown(x);
            });
        }

    }
}
