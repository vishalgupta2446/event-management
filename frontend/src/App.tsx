import { Route, Routes } from 'react-router-dom'
import { Layout } from './components/Layout'
import { EventDetailPage } from './pages/EventDetailPage'
import { CreateEventPage, EditEventPage } from './pages/EventFormPage'
import { EventsPage } from './pages/EventsPage'
import { NotFoundPage } from './pages/NotFoundPage'

export default function App() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route index element={<EventsPage />} />
        <Route path="events/new" element={<CreateEventPage />} />
        <Route path="events/:id" element={<EventDetailPage />} />
        <Route path="events/:id/edit" element={<EditEventPage />} />
        <Route path="*" element={<NotFoundPage />} />
      </Route>
    </Routes>
  )
}
