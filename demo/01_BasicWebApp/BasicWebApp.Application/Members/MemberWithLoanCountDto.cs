using System;
using System.Collections.Generic;

using BasicWebApp.Domain.Members;

namespace BasicWebApp.Application.Members;

public readonly record struct MemberWithLoanCountDto(Member Member, bool IsLoanSuspended, bool IsLoanSuspensionScheduled, int LoanCount);
