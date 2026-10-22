import { Link, Outlet } from 'react-router-dom'

export function Layout() {
  return (
    <>
      <header className="topbar">
        <div className="container topbar__inner">
          <Link to="/" className="brand">
            <span aria-hidden="true">📅</span> Eventful
          </Link>
          <Link to="/events/new" className="btn btn--primary btn--small">
            + New event
          </Link>
        </div>
      </header>
      <main className="container page">
        <Outlet />
      </main>
    </>
  )
}
