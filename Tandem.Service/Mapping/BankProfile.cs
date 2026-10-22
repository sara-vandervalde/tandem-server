using AutoMapper;
using Tandem.Core.DTOs.Banks;
using Tandem.Core.Entities;

namespace Tandem.Service.Mapping;

public class BankProfile : Profile
{
    public BankProfile()
    {
        CreateMap<Bank, BankDto>();

        // BankName is filled automatically from Bank.Name (AutoMapper "flattening": Bank + Name)
        CreateMap<BankAccount, BankAccountDto>();
    }
}
