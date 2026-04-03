using Lan.Dto;
using Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lan.Model.Vo
{
    public class TreeSelectVo
    {
        /// <summary>
        /// 节点Id
        /// </summary>
        public long Id { get; set; }
        /// <summary>
        /// 节点名称
        /// </summary>
        public string Label { get; set; }

        public TreeSelectVo() { }

        public TreeSelectVo(DefenceareaDtoParent dept)
        {
            Id = dept.Id;
            Label = dept.Name;

            List<TreeSelectVo> child = new List<TreeSelectVo>();
            foreach (var item in dept.children)
            {
                child.Add(new TreeSelectVo(item));
            }

            Children = child;
        }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<TreeSelectVo> Children { get; set; }
    }
}
