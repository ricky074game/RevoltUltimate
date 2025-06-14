import React from 'react'

import PropTypes from 'prop-types'

import './point.css'

const Point = (props) => {
  return (
    <div className="point-point">
      <div className="point-check">
        <svg viewBox="0 0 1024 1024" className="point-icon1">
          <path d="M384 690l452-452 60 60-512 512-238-238 60-60z"></path>
        </svg>
      </div>
      <span className="point-text">{props.text}</span>
    </div>
  )
}

Point.defaultProps = {
  text: 'Duis aute irure dolor in reprehenderit',
}

Point.propTypes = {
  text: PropTypes.string,
}

export default Point
