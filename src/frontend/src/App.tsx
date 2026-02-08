import './App.css'
import { FileUpload } from './components/FileUpload'
import { ChartView } from './components/ChartView'
import { RangeSelector } from './components/RangeSelector'
import { SlopeSummary } from './components/SlopeSummary'
import { ExportButton } from './components/ExportButton'

function App() {
  return (
    <div className="app">
      <header>
        <h1>Perfmon Analyzer</h1>
      </header>
      <main>
        <section className="upload-section">
          <FileUpload />
        </section>
        <section className="chart-section">
          <ChartView />
        </section>
        <section className="controls-section">
          <RangeSelector />
          <ExportButton />
        </section>
        <section className="results-section">
          <SlopeSummary />
        </section>
      </main>
    </div>
  )
}

export default App
