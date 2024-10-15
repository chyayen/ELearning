using PagedList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ELearning.Models
{
    public class ClassViewModel
    {
        public IPagedList<ClassModel> Classes { get; set; }
    }

    public class ClassModel
    {
        public int ID { get; set; } = 0;
        public string Name { get; set; } = "";
    }
}