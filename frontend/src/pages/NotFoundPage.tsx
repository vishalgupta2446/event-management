import { Link } from 'react-router-dom'
import { EmptyState } from '../components/Feedback'

export function NotFoundPage({ message = "The page you're looking for doesn't exist." }: { message?: string }) {
  return (
    <EmptyState title="Not found">
      <p className="muted">{message}</p>
      <Link to="/" className="btn btn--primary">Back to events</Link>
    </EmptyState>
  )
}
