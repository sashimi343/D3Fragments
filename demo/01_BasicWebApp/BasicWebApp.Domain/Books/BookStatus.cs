using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BasicWebApp.Domain.Books;

public enum BookStatus
{
    [Display(Name = "在架")]
    Available,

    [Display(Name = "貸出中")]
    OnLoan,

    [Display(Name = "取り置き中")]
    OnHold
}
