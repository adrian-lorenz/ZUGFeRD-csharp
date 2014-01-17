﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using System.IO;

namespace s2industries.ZUGFeRD
{
    internal class InvoiceDescriptorReader
    {
        public static InvoiceDescriptor Load(Stream stream)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(stream);
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.DocumentElement.OwnerDocument.NameTable);
            nsmgr.AddNamespace("rsm", doc.DocumentElement.OwnerDocument.DocumentElement.NamespaceURI);

            InvoiceDescriptor retval = new InvoiceDescriptor();
            retval.Profile = default(Profile).FromString(_nodeAsString(doc.DocumentElement, "//GuidelineSpecifiedDocumentContextParameter/ID", nsmgr));
            retval.Type = default(InvoiceType).FromString(_nodeAsString(doc.DocumentElement, "//rsm:HeaderExchangedDocument/TypeCode", nsmgr));
            retval.InvoiceNo = _nodeAsString(doc.DocumentElement, "//rsm:HeaderExchangedDocument/ID", nsmgr);
            retval.InvoiceDate = _nodeAsDateTime(doc.DocumentElement, "//rsm:HeaderExchangedDocument/IssueDateTime", nsmgr);

            foreach (XmlNode node in doc.SelectNodes("//rsm:HeaderExchangedDocument/IncludedNote", nsmgr))
            {
                string content = _nodeAsString(node, "//Content", nsmgr);
                string _subjectCode = _nodeAsString(node, "//SubjectCode", nsmgr);
                SubjectCodes subjectCode = default(SubjectCodes).FromString(_subjectCode);
                retval.AddNote(content, subjectCode);
            }

            retval.ReferenceOrderNo = _nodeAsString(doc, "//ApplicableSupplyChainTradeAgreement/BuyerReference", nsmgr);

            retval.Buyer = _nodeAsParty(doc.DocumentElement, "//ApplicableSupplyChainTradeAgreement/SellerTradeParty", nsmgr);
            foreach (XmlNode node in doc.SelectNodes("//ApplicableSupplyChainTradeAgreement/SellerTradeParty/SpecifiedTaxRegistration", nsmgr))
            {
                string id = _nodeAsString(node, "ID", nsmgr);
                string schemeID = _nodeAsString(node, "ID/@schemeID", nsmgr);

                retval.AddSellerTaxRegistration(id, default(TaxRegistrationSchemeID).FromString(schemeID));
            }

            retval.Seller = _nodeAsParty(doc.DocumentElement, "//ApplicableSupplyChainTradeAgreement/BuyerTradeParty", nsmgr);
            foreach (XmlNode node in doc.SelectNodes("//ApplicableSupplyChainTradeAgreement/BuyerTradeParty/SpecifiedTaxRegistration", nsmgr))
            {
                string id = _nodeAsString(node, "ID", nsmgr);
                string schemeID = _nodeAsString(node, "ID/@schemeID", nsmgr);

                retval.AddBuyerTaxRegistration(id, default(TaxRegistrationSchemeID).FromString(schemeID));
            }

            retval.ActualDeliveryDate = _nodeAsDateTime(doc.DocumentElement, "//ApplicableSupplyChainTradeDelivery/ActualDeliverySupplyChainEvent/OccurrenceDateTime", nsmgr);
            retval.DeliveryNoteNo = _nodeAsString(doc.DocumentElement, "//ApplicableSupplyChainTradeDelivery/DeliveryNoteReferencedDocument/ID", nsmgr);
            retval.DeliveryNoteDate = _nodeAsDateTime(doc.DocumentElement, "//ApplicableSupplyChainTradeDelivery/DeliveryNoteReferencedDocument/IssueDateTime", nsmgr);
            retval.InvoiceNoAsReference = _nodeAsString(doc.DocumentElement, "//ApplicableSupplyChainTradeSettlement/PaymentReference", nsmgr);
            retval.Currency = default(CurrencyCodes).FromString(_nodeAsString(doc.DocumentElement, "//ApplicableSupplyChainTradeSettlement/InvoiceCurrencyCode", nsmgr));

            foreach (XmlNode node in doc.SelectNodes("//ApplicableTradeTax", nsmgr))
            {
                retval.AddApplicableTradeTax(_nodeAsDecimal(node, "ActualAmount", nsmgr),
                                             _nodeAsDecimal(node, "BasisAmount", nsmgr),
                                             _nodeAsDecimal(node, "ApplicablePercent", nsmgr),
                                             default(TaxTypes).FromString(_nodeAsString(node, "TypeCode", nsmgr)),
                                             default(TaxCategoryCodes).FromString(_nodeAsString(node, "CategoryCode", nsmgr)));
            }

            foreach (XmlNode node in doc.SelectNodes("//SpecifiedTradeAllowanceCharge", nsmgr))
            {
                retval.AddTradeAllowanceCharge(_nodeAsBool(node, "ChargeIndicator", nsmgr),
                                               _nodeAsDecimal(node, "BasisAmount", nsmgr),
                                               retval.Currency,
                                               _nodeAsDecimal(node, "ActualAmount", nsmgr),
                                               _nodeAsString(node, "Reason", nsmgr),
                                               default(TaxTypes).FromString(_nodeAsString(node, "CategoryTradeTax/TypeCode", nsmgr)),
                                               default(TaxCategoryCodes).FromString(_nodeAsString(node, "CategoryTradeTax/CategoryCode", nsmgr)),
                                               _nodeAsDecimal(node, "CategoryTradeTax/ApplicablePercent", nsmgr));
            }

            foreach (XmlNode node in doc.SelectNodes("//SpecifiedLogisticsServiceCharge", nsmgr))
            {
                retval.AddLogisticsServiceCharge(_nodeAsDecimal(node, "AppliedAmount", nsmgr),
                                                 _nodeAsString(node, "Description", nsmgr),
                                                 default(TaxTypes).FromString(_nodeAsString(node, "AppliedTradeTax/TypeCode", nsmgr)),
                                                 default(TaxCategoryCodes).FromString(_nodeAsString(node, "AppliedTradeTax/CategoryCode", nsmgr)),
                                                 _nodeAsDecimal(node, "AppliedTradeTax/ApplicablePercent", nsmgr));
            }

            retval.PaymentTerms = new PaymentTerms()
            {
                Description = _nodeAsString(doc.DocumentElement, "//SpecifiedTradePaymentTerms/Description", nsmgr),
                DueDate = _nodeAsDateTime(doc.DocumentElement, "//SpecifiedTradePaymentTerms/DueDateDateTime", nsmgr)
            };

            retval.LineTotalAmount = _nodeAsDecimal(doc.DocumentElement, "//SpecifiedTradeSettlementMonetarySummation/LineTotalAmount", nsmgr);
            retval.ChargeTotalAmount = _nodeAsDecimal(doc.DocumentElement, "//SpecifiedTradeSettlementMonetarySummation/ChargeTotalAmount", nsmgr);
            retval.AllowanceTotalAmount = _nodeAsDecimal(doc.DocumentElement, "//SpecifiedTradeSettlementMonetarySummation/AllowanceTotalAmount", nsmgr);
            retval.TaxBasisAmount = _nodeAsDecimal(doc.DocumentElement, "//SpecifiedTradeSettlementMonetarySummation/TaxBasisTotalAmount", nsmgr);
            retval.TaxTotalAmount = _nodeAsDecimal(doc.DocumentElement, "//SpecifiedTradeSettlementMonetarySummation/TaxTotalAmount", nsmgr);
            retval.GrandTotalAmount = _nodeAsDecimal(doc.DocumentElement, "//SpecifiedTradeSettlementMonetarySummation/GrandTotalAmount", nsmgr);
            retval.TotalPrepaidAmount = _nodeAsDecimal(doc.DocumentElement, "//SpecifiedTradeSettlementMonetarySummation/TotalPrepaidAmount", nsmgr);
            retval.DuePayableAmount = _nodeAsDecimal(doc.DocumentElement, "//SpecifiedTradeSettlementMonetarySummation/DuePayableAmount", nsmgr);

            retval.OrderDate = _nodeAsDateTime(doc.DocumentElement, "//BuyerOrderReferencedDocument/IssueDateTime", nsmgr);
            retval.OrderNo = _nodeAsString(doc.DocumentElement, "//BuyerOrderReferencedDocument/ID", nsmgr);
            
            
            foreach(XmlNode node in doc.SelectNodes("//IncludedSupplyChainTradeLineItem"))
            {
                retval.TradeLineItems.Add(_parseTradeLineItem(node));
            }
            return retval;
        } // !Load()


        public static InvoiceDescriptor Load(string filename)
        {
            if (!System.IO.File.Exists(filename))
            {
                throw new FileNotFoundException();
            }

            return Load(new FileStream(filename, FileMode.Open, FileAccess.Read));
        } // !Load()


        private static TradeLineItem _parseTradeLineItem(XmlNode tradeLineItem, XmlNamespaceManager nsmgr = null)
        {
            if (tradeLineItem.SelectSingleNode("//AssociatedDocumentLineDocument/IncludedNote/Content") != null)
            {
                return new TradeLineItem()
                {
                    Comment = _nodeAsString(tradeLineItem, "//AssociatedDocumentLineDocument/IncludedNote/Content", nsmgr)
                };
            }
            else
            {
                return new TradeLineItem()
                {
                    GlobalID = new GlobalID(_nodeAsString(tradeLineItem, "//SpecifiedTradeProduct/GlobalID/@schemeID", nsmgr),
                                            _nodeAsString(tradeLineItem, "//SpecifiedTradeProduct/GlobalID", nsmgr)),
                    SellerAssignedID = _nodeAsString(tradeLineItem, "//SpecifiedTradeProduct/SellerAssignedID", nsmgr),
                    BuyerAssignedID = _nodeAsString(tradeLineItem, "//SpecifiedTradeProduct/BuyerAssignedID", nsmgr),
                    Name = _nodeAsString(tradeLineItem, "//SpecifiedTradeProduct/Name", nsmgr),
                    Description = _nodeAsString(tradeLineItem, "//SpecifiedTradeProduct/Description", nsmgr),
                    UnitQuantity = _nodeAsInt(tradeLineItem, "//BasisQuantity", nsmgr, 1),
                    BilledQuantity = _nodeAsInt(tradeLineItem, "//BilledQuantity", nsmgr, 1),
                    TaxCategoryCode = default(TaxCategoryCodes).FromString(_nodeAsString(tradeLineItem, "//ApplicableTradeTax/CategoryCode", nsmgr)),
                    TaxType = default(TaxTypes).FromString(_nodeAsString(tradeLineItem, "//ApplicableTradeTax/TypeCode", nsmgr)),
                    TaxPercent = _nodeAsDecimal(tradeLineItem, "//ApplicableTradeTax/ApplicablePercent", nsmgr),
                    NetUnitPrice = _nodeAsDecimal(tradeLineItem, "//GrossPriceProductTradePrice/ChargeAmount", nsmgr),
                    GrossUnitPrice = _nodeAsDecimal(tradeLineItem, "//NetPriceProductTradePrice/ChargeAmount", nsmgr),
                    UnitCode = default(QuantityCodes).FromString(_nodeAsString(tradeLineItem, "//BasisQuantity/@unitCode", nsmgr))
                };
            }
        } // !_parseTradeLineItem()


        private static bool _nodeAsBool(XmlNode node, string xpath, XmlNamespaceManager nsmgr = null, bool defaultValue = true)
        {
            string value = _nodeAsString(node, xpath, nsmgr);
            if (value == "")
            {
                return defaultValue;
            }
            else
            {
                if ((value.Trim().ToLower() == "true") || (value.Trim() == "1"))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        } // !_nodeAsBool()


        private static string _nodeAsString(XmlNode node, string xpath, XmlNamespaceManager nsmgr = null, string defaultValue = "")
        {
            try
            {
                XmlNode _node = node.SelectSingleNode(xpath, nsmgr);
                if (_node == null)
                {
                    return defaultValue;
                }
                else
                {
                    return _node.InnerText;
                }
            }
            catch (XPathException)
            {
                return defaultValue;
            }
            catch (Exception ex)
            {
                throw ex;
            };
        } // _nodeAsString()


        private static int _nodeAsInt(XmlNode node, string xpath, XmlNamespaceManager nsmgr = null, int defaultValue = 0)
        {
            string temp = _nodeAsString(node, xpath, nsmgr, "");
            int retval;
            if (Int32.TryParse(temp, out retval))
            {
                return retval;
            }
            else
            {
                return defaultValue;
            }
        } // !_nodeAsInt()


        private static decimal _nodeAsDecimal(XmlNode node, string xpath, XmlNamespaceManager nsmgr = null, decimal defaultValue = 0)
        {
            string temp = _nodeAsString(node, xpath, nsmgr, "");
            decimal retval;
            if (decimal.TryParse(temp, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out retval))
            {
                return retval;
            }
            else
            {
                return defaultValue;
            }
        } // !_nodeAsDecimal()


        private static DateTime _nodeAsDateTime(XmlNode node, string xpath, XmlNamespaceManager nsmgr = null)
        {
            string format = "102";
            try
            {
                format = node.Attributes["format"].InnerText;
            }
            catch
            {
            }

            if (format != "102")
            {
                throw new UnsupportedException();
            }
            string value = node.SelectSingleNode(xpath, nsmgr).InnerText;
            if (value.Length != 8)
            {
                throw new Exception("Wrong length of datetime element");
            }

            string year = value.Substring(0, 4);
            string month = value.Substring(4, 2);
            string day = value.Substring(6, 2);

            return new DateTime(Int32.Parse(year), Int32.Parse(month), Int32.Parse(day));
        } // !_nodeAsDateTime()


        private static Party _nodeAsParty(XmlNode baseNode, string xpath, XmlNamespaceManager nsmgr = null)
        {
            XmlNode node = baseNode.SelectSingleNode(xpath, nsmgr);

            return new Party()
            {
                ID = _nodeAsString(node, "//ID", nsmgr),
                GlobalID = new GlobalID(_nodeAsString(node, "//GlobalID/@schemeID", nsmgr),
                                        _nodeAsString(node, "//GlobalID", nsmgr)),
                Name = _nodeAsString(node, "//Name", nsmgr),
                Street = _nodeAsString(node, "//PostalTradeAddress/LineOne", nsmgr),
                Postcode = _nodeAsString(node, "//PostalTradeAddress/PostcodeCode", nsmgr),
                City = _nodeAsString(node, "//PostalTradeAddress/CityName", nsmgr),
                Country = _nodeAsString(node, "//PostalTradeAddress/CountryID", nsmgr)
            };
        } // !_nodeAsParty()
    }
}
