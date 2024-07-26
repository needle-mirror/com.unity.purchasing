#nullable enable

namespace UnityEngine.Purchasing
{
    class StoreCartValidator : ICartValidator
    {
        private readonly string m_StoreName;
        private readonly ICartValidator m_CartValidator;

        public StoreCartValidator(string storeName, ICartValidator cartValidator)
        {
            m_StoreName = storeName;
            m_CartValidator = cartValidator;
        }

        public void Validate(ICart cart)
        {
            try
            {
                m_CartValidator.Validate(cart);
            }
            catch (IapException iapException)
            {
                throw new InvalidCartException($"{m_StoreName} cart validation: {iapException.Message}");
            }
        }
    }
}
