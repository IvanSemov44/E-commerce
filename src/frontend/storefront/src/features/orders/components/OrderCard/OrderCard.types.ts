export interface OrderItemSummary {
  productName: string;
}

export interface Order {
  id: string;
  orderNumber: string;
  status: string;
  totalAmount: number;
  createdAt: string;
  items: OrderItemSummary[];
}

export interface OrderCardProps {
  order: Order;
}
